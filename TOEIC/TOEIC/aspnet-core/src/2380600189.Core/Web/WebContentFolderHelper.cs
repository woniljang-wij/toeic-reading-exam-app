using Abp.Reflection.Extensions;
using System;
using System.IO;
using System.Linq;

namespace TOEIC.Web;

/// <summary>
/// This class is used to find root path of the web project in;
/// unit tests (to find views) and entity framework core command line commands (to find conn string).
/// </summary>
public static class WebContentDirectoryFinder
{
    public static string CalculateContentRootFolder()
    {
        var coreAssemblyDirectoryPath = Path.GetDirectoryName(typeof(TOEICCoreModule).GetAssembly().Location);
        if (coreAssemblyDirectoryPath == null)
        {
            throw new Exception("Could not find location of TOEIC.Core assembly!");
        }

        var directoryInfo = new DirectoryInfo(coreAssemblyDirectoryPath);
        while (directoryInfo != null)
        {
            var sourceDirectory = Path.Combine(directoryInfo.FullName, "src");
            if (Directory.Exists(sourceDirectory))
            {
                foreach (var projectDirectory in Directory.GetDirectories(sourceDirectory))
                {
                    if (Directory.GetFiles(projectDirectory, "*.Web.Mvc.csproj").Length > 0)
                    {
                        return projectDirectory;
                    }
                }
            }

            directoryInfo = directoryInfo.Parent;
        }

        throw new Exception("Could not find root folder of the web project!");
    }
}
