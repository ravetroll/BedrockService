using System;
using System.Collections.Generic;
using System.Linq;

namespace BedrockService
{
    internal class BackupFile
    {
        public long Length { get; }

        public string Path { get; }

        private BackupFile(string path, long length)
        {
            Path = path;
            Length = length;
        }

        public static bool IsBackupSpecification(string input)
        {
            return input?.Contains(".ldb:") ?? false;
        }

        public static List<BackupFile> ParseBackupSpecification(string input)
        {
            return input
                .Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
                .Select(ParseFileSpec)
                .ToList();
        }

        private static BackupFile ParseFileSpec(string fileSpec)
        {
            var chunks = fileSpec.Split(':');

            // bedrock server returns paths with forward slashes,
            // but since we're on windows we need to convert them to backslashes
            var path = chunks[0].Replace('/', '\\');
            return new BackupFile(path, long.Parse(chunks[1]));
        }
    }
}