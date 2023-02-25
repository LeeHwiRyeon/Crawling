using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

public static class DuplicateImageChecker {

    public static void Check(AppConfig Config)
    {
        var duplicationPath = Config.StoragePath + @"\Duplication";
        if (Directory.Exists(duplicationPath) == false) {
            Directory.CreateDirectory(duplicationPath);
        }

        var hashList = new Dictionary<string, string>();
        // 디렉토리 내 모든 파일에 대해 중복 검사 수행
        foreach (var file in Directory.GetFiles(Config.StoragePath)) {
            var hash = GetImageHashMD5(file);

            if (hashList.ContainsKey(hash)) {
                Console.WriteLine($"중복된 이미지: {file} ({hash})");
                var newFilePath = Path.Combine(duplicationPath, Path.GetFileName(file));
                File.Move(file, newFilePath);
                // 중복 이미지의 파일 이름을 변경하여 구분할 수 있음
            } else {
                hashList.Add(hash, file);
            }
        }
    }

    private static string GetImageHashMD5(string filePath)
    {
        using (var md5 = MD5.Create()) {
            using (var stream = File.OpenRead(filePath)) {
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
