﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace SharpGLTF
{
    /// <summary>
    /// Wraps Khronos GLTF Validator command line tool.
    /// </summary>
    /// <see href="https://github.com/KhronosGroup/glTF-Validator"/>
    /// <remarks>
    /// LINUX execution path has not been tested!
    /// </remarks>
    public static class gltf_validator
    {
        static gltf_validator()
        {
            if (RuntimeInformation.OSArchitecture != Architecture.X64) return;

            ValidatorExePath = System.IO.Path.GetDirectoryName(typeof(gltf_validator).Assembly.Location);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ValidatorExePath = System.IO.Path.Combine(ValidatorExePath, "gltf_validator.exe");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                ValidatorExePath = System.IO.Path.Combine(ValidatorExePath, "gltf_validator");
            }
        }

        public static string ValidatorExePath { get; set; }

        public static ValidationReport ValidateFile(string gltfFilePath)
        {
            if (string.IsNullOrWhiteSpace(ValidatorExePath)) return null;

            if (!System.IO.File.Exists(ValidatorExePath)) throw new System.IO.FileNotFoundException(ValidatorExePath);

            if (!System.IO.Path.IsPathRooted(gltfFilePath)) gltfFilePath = System.IO.Path.GetFullPath(gltfFilePath);

            var psi = new System.Diagnostics.ProcessStartInfo(ValidatorExePath);
            psi.Arguments = $"-p -r -a --stdout \"{gltfFilePath}\"";
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;            

            using (var p = System.Diagnostics.Process.Start(psi))
            {
                // To avoid deadlocks, always read the output stream first and then wait.  
                var mainReport = p.StandardOutput.ReadToEnd();

                if (!p.WaitForExit(1000 * 10)) // wait for a reasonable timeout
                {
                    try { p.Kill(); } catch { return null; }
                }                

                if (string.IsNullOrWhiteSpace(mainReport)) return null;
                var report = ValidationReport.Parse(mainReport);
                
                if (report.Messages.Any(item => item.Code == "UNSUPPORTED_EXTENSION")) return null;

                return report;
            }
        }

        /// <summary>
        /// Represents the report generated by glTF validator
        /// </summary>
        /// <see href="https://github.com/KhronosGroup/glTF-Validator/blob/898f53944b40650aacef550c4977de04c46990ab/docs/validation.schema.json"/>
        public sealed class ValidationReport
        {
            public static ValidationReport Parse(string json)
            {
                var options = new JsonSerializerOptions();
                options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

                return JsonSerializer.Deserialize<ValidationReport>(json, options);
            }

            public string Uri { get; set; }
            public string MimeType { get; set; }
            public string ValidatorVersion { get; set; }
            public string ValidatedAt { get; set; }
            public ValidationIssues Issues { get; set; }
            public ValidationInfo Info { get; set; }

            public bool HasWarnings => Issues.NumWarnings > 0;
            public bool HasErrors => Issues.NumErrors > 0;

            public IEnumerable<ValidationMessage> Messages => Issues.Messages == null
                ? Enumerable.Empty<ValidationMessage>()
                : Issues.Messages;

            public IEnumerable<String> Hints => Messages
                .Where(item => item.Severity == 3)
                .Select(item => item.Message);

            public IEnumerable<String> Infos => Messages
                .Where(item => item.Severity == 2)
                .Select(item => item.Message);

            public IEnumerable<String> Warnings => Messages
                .Where(item => item.Severity == 1)
                .Select(item => item.Message);

            public IEnumerable<String> Errors => Messages
                .Where(item => item.Severity == 0)
                .Select(item => item.Message);

            public override string ToString()
            {
                var options = new JsonSerializerOptions();
                options.WriteIndented = true;
                options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                return JsonSerializer.Serialize(this, options);
            }
        }

        public sealed class ValidationIssues
        {
            public int NumErrors { get; set; }
            public int NumWarnings { get; set; }
            public int NumInfos { get; set; }
            public int NumHints { get; set; }
            public ValidationMessage[] Messages { get; set; }
            public bool Truncated { get; set; }
        }

        public sealed class ValidationMessage
        {
            public string Code { get; set; }
            public string Message { get; set; }
            public int Severity { get; set; }
            public string Pointer { get; set; }
            public int? Offset { get; set; }
        }

        public sealed class ValidationInfo
        {
            public string Version { get; set; }
            public string MinVersion { get; set; }
            public string Generator { get; set; }
            public string[] ExtensionsUsed { get; set; }
            public string[] ExtensionsRequired { get; set; }
            public ValidationResources[] Resources { get; set; }
            public int AnimationCount { get; set; }
            public int MaterialCount { get; set; }
            public bool HasMorphTargets { get; set; }
            public bool HasSkins { get; set; }
            public bool HasTextures { get; set; }
            public bool HasDefaultScene { get; set; }
            public int DrawCallCount { get; set; }
            public int TotalVertexCount { get; set; }
            public int TotalTriangleCount { get; set; }
            public int MaxUVs { get; set; }
            public int  MaxInfluences { get; set; }
            public int  MaxAttributes { get; set; }
        }

        public sealed class ValidationResources
        {
            public string Pointer { get; set; }
            public string Storage { get; set; }
            public string MimeType { get; set; }            
            public string Uri { get; set; }
            public int ByteLength { get; set; }
            public ValidationImage Image { get; set; }
        }

        public sealed class ValidationImage
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public string Format { get; set; }
            public string Primaries { get; set; }
            public string Transfer { get; set; }
            public int Bits { get; set; }
        }

        
    }
}
