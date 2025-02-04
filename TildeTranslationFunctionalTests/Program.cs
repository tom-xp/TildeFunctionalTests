using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tilde.Translation;
using Tilde.Translation.Exceptions;
using Tilde.Translation.Models;
using Tilde.Translation.Models.Document;
using TildeTranslationEnums = Tilde.Translation.Enums;

namespace TildeTranslationFunctionalTests
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Enter your Tilde Translation API Key:");
            string apiKey = Console.ReadLine();
            string serverUrl = "https://translate.staging.tilde.lv";

            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("API Key cannot be empty. Please run the application again and enter your API Key.");
                Console.ReadKey();
                return;
            }

            var options = new TranslatorOptions() { ServerUrl = serverUrl };

            using (var translator = new Translator(apiKey, options))
            {
                bool running = true;
                while (running)
                {
                    Console.WriteLine("\nChoose a test to run:");
                    Console.WriteLine("1. Text Translation Tests");
                    Console.WriteLine("2. Document Translation Test");
                    Console.WriteLine("3. List Engines");
                    Console.WriteLine("4. List Language Directions");
                    Console.WriteLine("5. Run All Tests");
                    Console.WriteLine("6. Exit");
                    Console.Write("Enter your choice (1-6): ");

                    string choice = Console.ReadLine();

                    switch (choice)
                    {
                        case "1":
                            await RunTextTranslationTests(translator);
                            break;
                        case "2":
                            await RunDocumentTranslationTest(translator);
                            break;
                        case "3":
                            await RunGetEnginesList(translator);
                            break;
                        case "4":
                            await RunGetLanguageDirectionsList(translator);
                            break;
                        case "5":
                            await RunAllTests(translator);
                            break;
                        case "6":
                            running = false;
                            break;
                        default:
                            Console.WriteLine("Invalid choice. Please enter a number between 1 and 6.");
                            break;
                    }
                }
            }

            Console.WriteLine("Exiting application. Press any key to exit.");
            Console.ReadKey();
        }

        static async Task RunGetLanguageDirectionsList(Translator translator)
        {
            Console.WriteLine("\nAvailable Language Directions (all):\n");
            try
            {
                int count = 0;
                await foreach (var languageDirection in translator.GetLanguageDirections())
                {
                    Console.WriteLine($"   {++count}. {languageDirection.SourceLanguage} -> {languageDirection.TargetLanguage} [{languageDirection.Domain ?? "General"}] | Engine: {languageDirection.EngineName} ({languageDirection.EngineVendor})");
                }
                if (count == 0)
                {
                    Console.WriteLine("   No language directions found.");
                }
                else
                {
                    Console.WriteLine($"\n   Listed {count} Language Directions (all shown).");
                }
            }
            catch (TildeException ex)
            {
                Console.WriteLine($"   Error getting language directions: {ex.GetType().Name} - {ex.Message}");
            }
            Console.WriteLine();
        }


        static async Task RunAllTests(Translator translator)
        {
            Console.WriteLine("\nRunning All Tests:\n");
            await RunTextTranslationTests(translator);
            await RunDocumentTranslationTest(translator);
            await RunGetEnginesList(translator);
            await RunGetLanguageDirectionsList(translator);
            Console.WriteLine("\nAll Tests Finished.");
        }

        static async Task RunTextTranslationTests(Translator translator)
        {
            Console.WriteLine("\nStarting Text Translation Tests...\n");

            Console.WriteLine("1.1. Translating single text 'First sentence' (en->lv)...");
            try
            {
                var translationResult = await translator.TranslateTextAsync("First sentence", "en", "lv");
                Console.WriteLine("   Translation Successful!");
                Console.WriteLine($"   Detected Language: {translationResult.DetectedLanguage}");
                Console.WriteLine($"   Domain: {translationResult.Domain}");
                Console.WriteLine($"   Translation: {translationResult.Translations[0].Translation}");
            }
            catch (TildeException ex)
            {
                Console.WriteLine($"   Translation Error: {ex.GetType().Name} - {ex.Message}");
            }
            Console.WriteLine();

            Console.WriteLine("1.2. Translating multiple texts (en->lv)...");
            try
            {
                var translationsResult = await translator.TranslateTextAsync(new[] { "Hello", "World" }, "en", "lv");
                Console.WriteLine("   Translation Successful!");
                Console.WriteLine($"   Translations:");
                foreach (var textResult in translationsResult.Translations)
                {
                    Console.WriteLine($"   - {textResult.Translation}");
                }
            }
            catch (TildeException ex)
            {
                Console.WriteLine($"   Translation Error: {ex.GetType().Name} - {ex.Message}");
            }
            Console.WriteLine();

            Console.WriteLine("1.3. Translating text with auto-detect (en->lv)...");
            try
            {
                var translationAutoDetectResult = await translator.TranslateTextAsync("This is English text", null, "lv");
                Console.WriteLine("   Translation Successful!");
                Console.WriteLine($"   Detected Language: {translationAutoDetectResult.DetectedLanguage}");
                Console.WriteLine($"   Translation: {translationAutoDetectResult.Translations[0].Translation}");
            }
            catch (TildeException ex)
            {
                Console.WriteLine($"   Translation Error: {ex.GetType().Name} - {ex.Message}");
            }
            Console.WriteLine();
        }

        static async Task RunDocumentTranslationTest(Translator translator)
        {
            Console.WriteLine("\nStarting Document Translation Test...\n");

            Console.Write("Single or Bulk document translation? (s/b): ");
            string translationMode = Console.ReadLine().ToLowerInvariant();
            bool isBulk = translationMode == "b";

            string sourcePath = "";
            string targetPath = "";

            if (isBulk)
            {
                Console.Write("Enter path to source directory containing document files: ");
                sourcePath = Console.ReadLine();
                Console.Write("Enter path to target directory to save translated documents: ");
                targetPath = Console.ReadLine();

                if (!Directory.Exists(sourcePath))
                {
                    Console.WriteLine($"Error: Source directory does not exist: {sourcePath}");
                    return;
                }
                if (!Directory.Exists(targetPath))
                {
                    Console.WriteLine($"Error: Target directory does not exist: {targetPath}");
                    return;
                }
            }
            else
            {
                Console.Write("Enter path to source document file (or press Enter for default './Document/ExampleDocument.txt'): ");
                sourcePath = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(sourcePath))
                {
                    sourcePath = "./Document/ExampleDocument.txt";
                }

                Console.Write("Enter path to save translated document (or press Enter for default './Document/ExampleDocumentResult.txt'): ");
                targetPath = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(targetPath))
                {
                    targetPath = "./Document/ExampleDocumentResult.txt";
                }


                if (!System.IO.File.Exists(sourcePath))
                {
                    Console.WriteLine($"Creating default document: {sourcePath}");
                    Directory.CreateDirectory("Document");
                    System.IO.File.WriteAllText(sourcePath, "This is example document\nTo be translated.");
                }
                else if (!Directory.Exists(Path.GetDirectoryName(sourcePath)))
                {
                    Console.WriteLine($"Error: Directory for source document does not exist: {Path.GetDirectoryName(sourcePath)}");
                    return;
                }

                if (!Directory.Exists(Path.GetDirectoryName(targetPath)))
                {
                    Console.WriteLine($"Error: Directory for target document does not exist: {Path.GetDirectoryName(targetPath)}");
                    return;
                }
            }


            try
            {
                if (isBulk)
                {
                    Console.WriteLine("\nStarting BULK document translation from: {sourcePath} to {targetPath} (en->lv)...");
                    string[] sourceFiles = Directory.GetFiles(sourcePath, "*.txt");
                    if (sourceFiles.Length == 0)
                    {
                        Console.WriteLine($"   No .txt files found in source directory: {sourcePath}");
                        return;
                    }

                    foreach (string sourceFileFullPath in sourceFiles)
                    {
                        string sourceFileName = Path.GetFileName(sourceFileFullPath);
                        string targetFileFullPath = Path.Combine(targetPath, Path.GetFileNameWithoutExtension(sourceFileName) + "_translated.txt");

                        Console.WriteLine($"\n   Translating document: {sourceFileName} ...");
                        FileInfo sourceDocument = new FileInfo(sourceFileFullPath);

                        try
                        {
                            using (FileStream fs = sourceDocument.OpenRead()) { }
                        }
                        catch (UnauthorizedAccessException)
                        {
                            Console.WriteLine($"   Error: No read permissions for source file: {sourceFileName}");
                            continue;
                        }


                        DocumentHandle documentHandle = await translator.TranslateDocumentAsync(sourceDocument, "en", "lv");
                        Console.WriteLine("      Translation started. Waiting for completion...");
                        DocumentStatus documentStatus = await translator.TranslateDocumentWaitUntilDoneAsync(documentHandle);

                        if (documentStatus.Status == TildeTranslationEnums.Document.TranslationStatus.Completed)
                        {
                            try
                            {
                                using (FileStream fileStream = System.IO.File.Create(targetFileFullPath))
                                {
                                    await translator.TranslateDocumentResultAsync(documentHandle, fileStream);
                                }
                            }
                            catch (UnauthorizedAccessException)
                            {
                                Console.WriteLine($"   Error: No write permissions for target directory: {targetPath} or file: {Path.GetFileNameWithoutExtension(sourceFileName) + "_translated.txt"}");
                                continue;
                            }
                            Console.WriteLine($"      Translation of {sourceFileName} saved to: {targetFileFullPath}");
                        }
                        else
                        {
                            Console.WriteLine($"      Translation of {sourceFileName} failed with status: {documentStatus.Status} - {documentStatus.Substatus}");
                        }
                    }
                    Console.WriteLine("\n   BULK document translation finished.");

                }
                else
                {
                    Console.WriteLine($"\nTranslating document: {sourcePath} to {targetPath} (en->lv)...");
                    FileInfo sourceDocument = new FileInfo(sourcePath);

                    try
                    {
                        using (FileStream fs = sourceDocument.OpenRead()) { }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Console.WriteLine($"   Error: No read permissions for source file: {sourcePath}");
                        return;
                    }

                    DocumentHandle documentHandle = await translator.TranslateDocumentAsync(sourceDocument, "en", "lv");

                    Console.WriteLine("   Document translation started. Waiting for completion...");
                    DocumentStatus documentStatus = await translator.TranslateDocumentWaitUntilDoneAsync(documentHandle);

                    Console.WriteLine($"   Document Status: {documentStatus.Status}");
                    Console.WriteLine($"   Document Substatus: {documentStatus.Substatus}");

                    if (documentStatus.Status == TildeTranslationEnums.Document.TranslationStatus.Completed)
                    {
                        try
                        {
                            using (FileStream fileStream = System.IO.File.Create(targetPath))
                            {
                                await translator.TranslateDocumentResultAsync(documentHandle, fileStream);
                            }
                        }
                        catch (UnauthorizedAccessException)
                        {
                            Console.WriteLine($"   Error: No write permissions for target file: {targetPath}");
                            return;
                        }
                        Console.WriteLine($"   Document translation saved to: {targetPath}");
                    }
                    else
                    {
                        Console.WriteLine($"   Document translation failed with status: {documentStatus.Status} - {documentStatus.Substatus}");
                    }
                }
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"   Error: Source document file not found: {sourcePath}");
                Console.WriteLine($"   Details: {ex.Message}");
            }
            catch (DirectoryNotFoundException ex)
            {
                Console.WriteLine($"   Error: Directory not found in path: {sourcePath} or {targetPath}");
                Console.WriteLine($"   Details: {ex.Message}");
            }
            catch (TildeException ex)
            {
                Console.WriteLine($"   Document Translation Error: {ex.GetType().Name} - {ex.Message}");
            }
            Console.WriteLine();
        }


        static async Task RunGetEnginesList(Translator translator)
        {
            Console.WriteLine("\nAvailable Engines (all):\n");
            try
            {
                var engines = await translator.GetEnginesAsync();
                Console.WriteLine("   Engines Retrieved Successfully!");
                if (engines.Any())
                {
                    int count = 0;
                    foreach (var engine in engines)
                    {
                        Console.WriteLine($"   {++count}. Engine Name: {engine.Name}");
                        Console.WriteLine($"      Vendor: {engine.EngineVendor}");
                        Console.WriteLine($"      Source Languages: {string.Join(", ", engine.SourceLanguages)}");
                        Console.WriteLine($"      Target Languages: {string.Join(", ", engine.TargetLanguages)}");
                        Console.WriteLine($"      Domain: {engine.Domain ?? "General"}");
                        Console.WriteLine($"      Status: {engine.Status}");
                        Console.WriteLine($"      Supports Term Collections: {engine.SupportsTermCollections}");
                        Console.WriteLine($"      Engine ID: {engine.Id}");
                        Console.WriteLine();
                    }
                    Console.WriteLine($"\n   Listed {count} Engines (all shown).");
                }
                else
                {
                    Console.WriteLine("   No engines found.");
                }
            }
            catch (TildeException ex)
            {
                Console.WriteLine($"   Get Engines Error: {ex.GetType().Name} - {ex.Message}");
            }
            Console.WriteLine();
        }
    }
}