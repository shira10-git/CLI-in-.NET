using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text;

// הגדרת תיקיות שיש לדלג עליהן
var excludedDirectories = new[] { "bin", "debug", "obj" };

// הגדרת אפשרויות
var outputOption = new Option<FileInfo>("--output", "File path and name") { IsRequired = true };
outputOption.AddAlias("-o");

var languageOption = new Option<string>("--language", "List of programming languages (e.g., C#, Java, Python, etc.)") { IsRequired = true };
languageOption.AddAlias("-l");

var noteOption = new Option<bool>("--note", "Add source file information as comments.");
noteOption.AddAlias("-n");

var sortOption = new Option<string>("--sort", () => "name", "Sort by file name or type.");
sortOption.AddAlias("-s");

var removeEmptyLinesOption = new Option<bool>("--remove-empty-lines", "Remove empty lines.");
removeEmptyLinesOption.AddAlias("-r");

var authorOption = new Option<string>("--author", "Author name to add as a comment.");
authorOption.AddAlias("-a");

// הגדרת פקודה
var bundleCommand = new Command("bundle", "Bundle code files to a single file.")
{
    outputOption,
    languageOption,
    noteOption,
    sortOption,
    removeEmptyLinesOption,
    authorOption
};

// מימוש הלוגיקה של עיבוד הפקודה
bundleCommand.SetHandler<FileInfo, string, bool, string, bool, string>(
    (output, language, note, sort, removeEmptyLines, author) =>
    {
        try
        {
            // רשימת שפות נתמכות
            var supportedLanguages = new[] { "cs", "c", "cpp", "js", "jsx", "py", "java", "txt" };

            // פענוח שפות מהאפשרות
            var selectedLanguages = language.Equals("all", StringComparison.OrdinalIgnoreCase)
                ? supportedLanguages
                : language.Split(',', StringSplitOptions.RemoveEmptyEntries)
                          .Select(lang => lang.Trim().ToLower())
                          .Where(lang => supportedLanguages.Contains(lang))
                          .ToArray();

            if (!selectedLanguages.Any())
            {
                Console.WriteLine("No valid languages selected. Supported languages are: " + string.Join(", ", supportedLanguages));
                return;
            }

            // איתור כל הקבצים בתיקייה הנוכחית שעונים לקריטריונים
            var files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.*", SearchOption.AllDirectories)
                .Where(file =>
                {
                    var relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), file);
                    var directoryName = Path.GetDirectoryName(relativePath)?.Split(Path.DirectorySeparatorChar).FirstOrDefault();
                    return selectedLanguages.Contains(Path.GetExtension(file).TrimStart('.').ToLower()) &&
                           !excludedDirectories.Contains(directoryName?.ToLower());
                })
                .ToList();

            if (!files.Any())
            {
                Console.WriteLine("No files found matching the selected languages and criteria.");
                return;
            }

            // מיון קבצים
            files = sort switch
            {
                "name" => files.OrderBy(Path.GetFileName).ToList(),
                "type" => files.OrderBy(Path.GetExtension).ThenBy(Path.GetFileName).ToList(),
                _ => files
            };

            // יצירת קובץ הפלט ואיחוד הקבצים לתוכו
            using var writer = new StreamWriter(output.FullName, false, Encoding.UTF8);

            if (!string.IsNullOrWhiteSpace(author))
            {
                writer.WriteLine($"// Author: {author}");
                writer.WriteLine();
            }

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);

                // הוספת הערה עם שם הקובץ לפני התוכן
                writer.WriteLine($"// Start of file: {fileName}");
                writer.WriteLine();

                if (note)
                {
                    var relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), file);
                    writer.WriteLine($"// Relative path: {relativePath}");
                    writer.WriteLine();
                }

                var lines = File.ReadAllLines(file);

                if (removeEmptyLines)
                {
                    lines = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
                }

                foreach (var line in lines)
                {
                    writer.WriteLine(line);
                }

                // הוספת הערה עם שם הקובץ אחרי התוכן
                writer.WriteLine();
                writer.WriteLine($"// End of file: {fileName}");
                writer.WriteLine(); // הוספת שורה ריקה בין קבצים
            }

            Console.WriteLine($"Bundle created successfully at {output.FullName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    },
    outputOption, languageOption, noteOption, sortOption, removeEmptyLinesOption, authorOption
);


var createRspCommand = new Command("create-rsp", "Create a response file for the bundle command.");
createRspCommand.SetHandler(() =>
{
    try {
        Console.Write("Enter output file path and name (e.g., output.txt): ");
        var output = Console.ReadLine() ?? string.Empty;

        Console.Write("Enter programming languages (comma-separated, or 'all'): ");
        var languages = Console.ReadLine() ?? string.Empty;

        Console.Write("Include source file information as comments? (yes/no): ");
        var includeNotes = Console.ReadLine()?.Trim().ToLower() == "yes";

        Console.Write("Sort files by 'name' or 'type' (default: name): ");
        var sort = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(sort)) sort = "name";

        Console.Write("Remove empty lines? (yes/no): ");
        var removeEmptyLines = Console.ReadLine()?.Trim().ToLower() == "yes";

        Console.Write("Enter author name (optional): ");
        var author = Console.ReadLine();

        // יצירת תוכן קובץ התגובה
        var responseFileContent = new StringBuilder();
        responseFileContent.AppendLine($"--output {output}");
        responseFileContent.AppendLine($"--language {languages}");
        if (includeNotes) responseFileContent.AppendLine("--note");
        responseFileContent.AppendLine($"--sort {sort}");
        if (removeEmptyLines) responseFileContent.AppendLine("--remove-empty-lines");
        if (!string.IsNullOrWhiteSpace(author)) responseFileContent.AppendLine($"--author \"{author}\"");

        // שמירת קובץ התגובה
        var responseFileName = "bundle.rsp";
        File.WriteAllText(responseFileName, responseFileContent.ToString());

        Console.WriteLine($"Response file created: {responseFileName}");
        Console.WriteLine($"To use it, run: fib bundle @bundle.rsp");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }

});


// הגדרת RootCommand
var rootCommand = new RootCommand("Root command for File Bundler CLI");
rootCommand.AddCommand(bundleCommand);
rootCommand.AddCommand(createRspCommand);

// הפעלת RootCommand
await rootCommand.InvokeAsync(args);



