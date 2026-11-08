using System;
using System.Collections.Generic;
using System.IO;
using MetadataReferenceResolver = EVI_ID.Mdl.MetadataReferenceResolver;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace EVI_ID.Testing;

public static partial class UnitTestRunner
{

    private static void RunMetadataReferenceResolverTests(
        List<UnitTestResult> results)
    {
        Run(
            results,
            "MetadataReferenceResolver: null bytes",
            () =>
            {
                bool rejected = false;

                try
                {
                    MetadataReferenceResolver.FromBytes(null!);
                }
                catch (ArgumentNullException)
                {
                    rejected = true;
                }

                AssertTrue(
                    rejected,
                    "Null assembly image was accepted.");
            });

        Run(
            results,
            "MetadataReferenceResolver: empty bytes",
            () =>
            {
                bool rejected = false;

                try
                {
                    MetadataReferenceResolver.FromBytes(
                        Array.Empty<byte>());
                }
                catch (ArgumentException)
                {
                    rejected = true;
                }

                AssertTrue(
                    rejected,
                    "Empty assembly image was accepted.");
            });

        Run(
            results,
            "MetadataReferenceResolver: empty path",
            () =>
            {
                bool rejected = false;

                try
                {
                    MetadataReferenceResolver.FromFile(" ");
                }
                catch (ArgumentException)
                {
                    rejected = true;
                }

                AssertTrue(
                    rejected,
                    "Empty file path was accepted.");
            });

        Run(
            results,
            "MetadataReferenceResolver: missing file",
            () =>
            {
                string missingPath = Path.Combine(
                    Path.GetTempPath(),
                    "EVI-ID-missing-" +
                    Guid.NewGuid().ToString("N") +
                    ".dll");

                bool rejected = false;

                try
                {
                    MetadataReferenceResolver.FromFile(
                        missingPath);
                }
                catch (FileNotFoundException)
                {
                    rejected = true;
                }

                AssertTrue(
                    rejected,
                    "Missing reference file was accepted.");
            });

        Run(
            results,
            "MetadataReferenceResolver: valid bytes",
            () =>
            {
                byte[] image = ResolverFixtureData.GetImage();

                var reference =
                    MetadataReferenceResolver.FromBytes(
                        image,
                        "ResolverFixture.dll");

                AssertTrue(
                    reference is not null,
                    "Reference was not created.");

                AssertEqual(
                    "ResolverFixture.dll",
                    reference.FilePath);
            });

        Run(
            results,
            "MetadataReferenceResolver: valid file",
            () =>
            {
                byte[] image = ResolverFixtureData.GetImage();

                string directory = Path.Combine(
                    Path.GetTempPath(),
                    "EVI-ID-reference-test-" +
                    Guid.NewGuid().ToString("N"));

                Directory.CreateDirectory(directory);

                string path = Path.Combine(
                    directory,
                    "ResolverFixture.dll");

                try
                {
                    File.WriteAllBytes(path, image);

                    var reference =
                        MetadataReferenceResolver.FromFile(path);

                    AssertTrue(
                        reference is not null,
                        "File reference was not created.");

                    AssertEqual(
                        path,
                        reference.FilePath);
                }
                finally
                {
                    Directory.Delete(
                        directory,
                        recursive: true);
                }
            });

        Run(
            results,
            "MetadataReferenceResolver: type resolution",
            () =>
            {
                byte[] image = ResolverFixtureData.GetImage();

                var reference =
                    MetadataReferenceResolver.FromBytes(image);

                var compilation =
                    CSharpCompilation.Create(
                        "ResolverConsumer",
                        references: new MetadataReference[]
                        {
                            reference
                        },
                        options: new CSharpCompilationOptions(
                            OutputKind.DynamicallyLinkedLibrary));

                var symbol =
                    compilation.GetTypeByMetadataName(
                        "IResolverFixture");

                AssertTrue(
                    symbol is not null,
                    "Referenced IResolverFixture type was not resolved.");

                AssertEqual(
                    "ResolverFixture",
                    symbol!.ContainingAssembly.Name);
            });
    }

}
