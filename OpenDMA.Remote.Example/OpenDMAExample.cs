using OpenDMA.Api;
using OpenDMA.Remote;

namespace OpenDMA.Remote.Example
{
    class OpenDMAExample
    {
        static void Main(string[] args)
        {
            string endpoint = args.Length > 0 ? args[0] : "http://127.0.0.1:8080/opendma/";
            
            Console.WriteLine($"Connecting to OpenDMA service at {endpoint}...");
            
            try
            {
                var session = RemoteSessionFactory.Connect(endpoint, "tutorialuser", "tutorialpwd", requestTraceLevel: 0);
                
                Console.WriteLine("Connected successfully!");
                Console.WriteLine($"Repositories: {string.Join(", ", session.GetRepositoryIds())}");
                
                var repoId = session.GetRepositoryIds()[0];
                Console.WriteLine($"\nFetching repository {repoId}...");
                var repo = session.GetRepository(repoId);
                
                Console.WriteLine($"\n=== Repository Details ===");
                Console.WriteLine($"  ID: {repo.Id}");
                Console.WriteLine($"  Name: {repo.Name}");
                Console.WriteLine($"  Display Name: {repo.DisplayName}");
                
                // Explore class tree
                Console.WriteLine("\n=== Class Hierarchy ===");
                PrintClassTree(repo.RootClass, 0);
                
                // Explore folder tree
                if (repo.RootFolder != null)
                {
                    Console.WriteLine("\n=== Folder Structure ===");
                    PrintFolderTree(repo.RootFolder, 0);
                }
                else
                {
                    Console.WriteLine("\n=== No root folder in this repository ===");
                }
                
                session.Close();
                Console.WriteLine("\nSession closed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"  Inner: {ex.InnerException.Message}");
                }
            }
        }
        
        static void PrintClassTree(IOdmaClass clazz, int indent)
        {
            var indentStr = new string(' ', indent * 2);
            Console.WriteLine($"{indentStr}{clazz.QName}");

            var aspects = clazz.Aspects;
            if (aspects != null)
            {
                foreach (var aspect in aspects)
                {
                    Console.WriteLine($"{indentStr}  @{aspect.QName}");
                }
            }

            var subClasses = clazz.SubClasses;
            if (subClasses != null)
            {
                foreach (var subClass in subClasses)
                {
                    PrintClassTree(subClass, indent + 1);
                }
            }
        }

        static void PrintFolderTree(IOdmaFolder folder, int indent)
        {
            var indentStr = new string(' ', indent * 2);
            Console.WriteLine($"{indentStr}{folder.Title}");

            var associations = folder.Associations;
            if (associations != null)
            {
                foreach (var assoc in associations)
                {
                    Console.WriteLine($"{indentStr}  -{assoc.Name}");
                    PrintObjectInfo(assoc.Containable, indent + 2);
                }
            }

            var subFolders = folder.SubFolders;
            if (subFolders != null)
            {
                foreach (var subFolder in subFolders)
                {
                    PrintFolderTree(subFolder, indent + 1);
                }
            }
        }

        static void PrintObjectInfo(IOdmaObject obj, int indent)
        {
            var indentStr = new string(' ', indent * 2);
            try
            {
                Console.WriteLine($"{indentStr}{obj.Id} ({obj.OdmaClass.QName})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{indentStr}[Error: {ex.Message}]");
            }
        }
    }
}
