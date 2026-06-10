using Vault.Client;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Text.Json;

namespace APIGateway.Security;

public interface ISecretManager
{
    Task<string?> GetSecretAsync(string key);
    Task SetSecretAsync(string key, string value);
    Task RotateSecretAsync(string key);
}

public class VaultSecretManager : ISecretManager
{
    private readonly VaultClient _vaultClient;
    private readonly string _path;
    private readonly ILogger<VaultSecretManager> _logger;

    public VaultSecretManager(IConfiguration configuration, ILogger<VaultSecretManager> logger)
    {
        _logger = logger;
        _path = configuration["Vault:Path"] ?? "secret/apigateway";

        var vaultAddress = configuration["Vault:Address"] ?? "http://localhost:8200";
        var vaultToken = configuration["Vault:Token"] ?? "";

        var authMethod = new TokenAuthMethodInfo(vaultToken);
        var vaultClientSettings = new VaultClientSettings(vaultAddress, authMethod);
        _vaultClient = new VaultClient(vaultClientSettings);
    }

    public async Task<string?> GetSecretAsync(string key)
    {
        try
        {
            var secretPath = $"{_path}/{key}";
            var secret = await _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(secretPath);

            if (secret?.Data?.Data != null && secret.Data.Data.TryGetValue("value", out var value))
            {
                return value?.ToString();
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving secret {Key} from Vault", key);
            return null;
        }
    }

    public async Task SetSecretAsync(string key, string value)
    {
        try
        {
            var secretPath = $"{_path}/{key}";
            var data = new Dictionary<string, object> { { "value", value } };
            await _vaultClient.V1.Secrets.KeyValue.V2.WriteSecretAsync(secretPath, data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting secret {Key} in Vault", key);
        }
    }

    public async Task RotateSecretAsync(string key)
    {
        var newValue = GenerateSecureToken();
        await SetSecretAsync(key, newValue);
    }

    private string GenerateSecureToken()
    {
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}

public class AwsSecretsManager : ISecretManager
{
    private readonly IAmazonSecretsManager _secretsManager;
    private readonly ILogger<AwsSecretsManager> _logger;

    public AwsSecretsManager(ILogger<AwsSecretsManager> logger)
    {
        _logger = logger;
        _secretsManager = new AmazonSecretsManagerClient();
    }

    public async Task<string?> GetSecretAsync(string key)
    {
        try
        {
            var request = new GetSecretValueRequest { SecretId = key };
            var response = await _secretsManager.GetSecretValueAsync(request);
            return response.SecretString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving secret {Key} from AWS Secrets Manager", key);
            return null;
        }
    }

    public async Task SetSecretAsync(string key, string value)
    {
        try
        {
            var request = new UpdateSecretRequest
            {
                SecretId = key,
                SecretString = value
            };
            await _secretsManager.UpdateSecretAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting secret {Key} in AWS Secrets Manager", key);
        }
    }

    public async Task RotateSecretAsync(string key)
    {
        var newValue = Guid.NewGuid().ToString();
        await SetSecretAsync(key, newValue);
    }
}

public class EncryptionService
{
    public static string Encrypt(string plainText, string key)
    {
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = Convert.FromBase64String(key);
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        using var msEncrypt = new MemoryStream();
        using var csEncrypt = new System.Security.Cryptography.CryptoStream(msEncrypt, encryptor, System.Security.Cryptography.CryptoStreamMode.Write);
        using var swEncrypt = new StreamWriter(csEncrypt);

        swEncrypt.Write(plainText);
        swEncrypt.Flush();
        csEncrypt.FlushFinalBlock();

        var iv = aes.IV;
        var encrypted = msEncrypt.ToArray();

        var result = new byte[iv.Length + encrypted.Length];
        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
        Buffer.BlockCopy(encrypted, 0, result, iv.Length, encrypted.Length);

        return Convert.ToBase64String(result);
    }

    public static string Decrypt(string cipherText, string key)
    {
        var fullCipher = Convert.FromBase64String(cipherText);

        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = Convert.FromBase64String(key);

        var iv = new byte[aes.BlockSize / 8];
        var cipher = new byte[fullCipher.Length - iv.Length];

        Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        using var msDecrypt = new MemoryStream(cipher);
        using var csDecrypt = new System.Security.Cryptography.CryptoStream(msDecrypt, decryptor, System.Security.Cryptography.CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);

        return srDecrypt.ReadToEnd();
    }
}