using System.Security.Cryptography;
using System.Text;

namespace MsgPulse.Api.Services;

/// <summary>
/// 配置加密服务 - 用于加密存储敏感配置信息
/// </summary>
public class ConfigurationEncryptionService
{
    private readonly byte[] _key;
    private readonly ILogger<ConfigurationEncryptionService> _logger;

    public ConfigurationEncryptionService(
        IConfiguration configuration,
        ILogger<ConfigurationEncryptionService> logger)
    {
        _logger = logger;

        // 从环境变量或配置文件加载加密密钥
        var keyString = configuration["Encryption:Key"] ??
                       Environment.GetEnvironmentVariable("MSGPULSE_ENCRYPTION_KEY");

        if (string.IsNullOrEmpty(keyString))
        {
            // 开发环境使用默认密钥（生产环境必须配置）
            _logger.LogWarning("未配置加密密钥，使用默认密钥（仅适用于开发环境）");
            keyString = "MsgPulse-Default-Key-For-Development-Only-32Bytes!";
        }

        // 确保密钥长度为32字节（AES-256）
        _key = DeriveKey(keyString, 32);
    }

    /// <summary>
    /// 加密字符串
    /// </summary>
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        try
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // 将IV和加密数据组合（IV + Encrypted Data）
            var result = new byte[aes.IV.Length + encryptedBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

            return Convert.ToBase64String(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加密配置时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 解密字符串
    /// </summary>
    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        try
        {
            var data = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            aes.Key = _key;

            // 提取IV（前16字节）
            var iv = new byte[16];
            var encryptedData = new byte[data.Length - 16];
            Buffer.BlockCopy(data, 0, iv, 0, 16);
            Buffer.BlockCopy(data, 16, encryptedData, 0, encryptedData.Length);

            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            var decryptedBytes = decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);

            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解密配置时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 判断字符串是否已加密
    /// </summary>
    public bool IsEncrypted(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        try
        {
            // 尝试Base64解码
            var data = Convert.FromBase64String(value);

            // 加密数据至少包含16字节IV + 至少1块加密数据（16字节）
            if (data.Length < 32)
                return false;

            // 尝试解密验证
            using var aes = Aes.Create();
            aes.Key = _key;

            var iv = new byte[16];
            Buffer.BlockCopy(data, 0, iv, 0, 16);
            aes.IV = iv;

            var encryptedData = new byte[data.Length - 16];
            Buffer.BlockCopy(data, 16, encryptedData, 0, encryptedData.Length);

            using var decryptor = aes.CreateDecryptor();
            decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 从字符串派生固定长度的密钥
    /// </summary>
    private byte[] DeriveKey(string password, int keyLength)
    {
        // 使用PBKDF2派生密钥
        using var deriveBytes = new Rfc2898DeriveBytes(
            password,
            Encoding.UTF8.GetBytes("MsgPulse-Salt"), // 固定盐值
            10000, // 迭代次数
            HashAlgorithmName.SHA256);

        return deriveBytes.GetBytes(keyLength);
    }

    /// <summary>
    /// 加密JSON配置（如果尚未加密）
    /// </summary>
    public string EncryptIfNeeded(string configuration)
    {
        if (string.IsNullOrEmpty(configuration))
            return configuration;

        if (IsEncrypted(configuration))
        {
            _logger.LogDebug("配置已加密，跳过");
            return configuration;
        }

        _logger.LogInformation("检测到明文配置，正在加密");
        return Encrypt(configuration);
    }

    /// <summary>
    /// 解密JSON配置（如果已加密）
    /// </summary>
    public string DecryptIfNeeded(string configuration)
    {
        if (string.IsNullOrEmpty(configuration))
            return configuration;

        if (!IsEncrypted(configuration))
        {
            _logger.LogDebug("配置未加密，直接返回");
            return configuration;
        }

        return Decrypt(configuration);
    }
}
