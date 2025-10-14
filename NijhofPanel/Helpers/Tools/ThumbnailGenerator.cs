namespace NijhofPanel.Helpers.Tools;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using Views;
using System.Security.Cryptography;
using System.Text;

[Transaction(TransactionMode.Manual)]
public class ThumbnailGenerator : IExternalCommand
{
    public Result Execute(
        ExternalCommandData commandData,
        ref string message,
        ElementSet elements)
    {
        try
        {
            Run(commandData.Application);
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }

    public static void Run(UIApplication uiApp)
    {
        string sourceDir = @"F:\Stabiplan\Custom\Families";
        string cacheDir = @"F:\Revit\Nijhof Tools\cache";
        Directory.CreateDirectory(cacheDir);

        var app = uiApp.Application;
        var families = Directory.GetFiles(sourceDir, "*.rfa", SearchOption.AllDirectories);
        int total = families.Length;
        int success = 0, failed = 0;

        if (total == 0)
        {
            TaskDialog.Show("Thumbnails", "Geen families gevonden in de bronmap.");
            return;
        }

        // Toon het voortgangsvenster via de WPF-dispatcher
        ProgressWindowView progressWindow = null!;
        System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
        {
            progressWindow = new ProgressWindowView();
            progressWindow.Show();
        });

        int index = 0;
        foreach (string file in families)
        {
            index++;
            string cacheFile = Path.Combine(cacheDir, GetCacheFileName(file));

            // Update voortgangsvenster
            int percentage = (int)((double)index / total * 100);
            progressWindow?.UpdateProgress(percentage);
            progressWindow?.UpdateStatusText($"Verwerken: {Path.GetFileName(file)}");

            uiApp.Application.WriteJournalComment($"Thumbnail {index}/{total}: {Path.GetFileName(file)}", false);

            try
            {
                if (File.Exists(cacheFile))
                    continue;

                var modelPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(file);
                var opts = new OpenOptions { DetachFromCentralOption = DetachFromCentralOption.DoNotDetach };

                using (Document doc = app.OpenDocumentFile(modelPath, opts))
                {
                    if (doc == null) { failed++; continue; }

                    // Zoek of maak een 3D-view
                    var view = new FilteredElementCollector(doc)
                        .OfClass(typeof(View3D))
                        .Cast<View3D>()
                        .FirstOrDefault(v => !v.IsTemplate);

                    if (view == null)
                    {
                        ViewFamilyType viewFamilyType = new FilteredElementCollector(doc)
                            .OfClass(typeof(ViewFamilyType))
                            .Cast<ViewFamilyType>()
                            .FirstOrDefault(x => x.ViewFamily == ViewFamily.ThreeDimensional);

                        using (Transaction tx = new Transaction(doc, "Create 3D View"))
                        {
                            tx.Start();
                            view = View3D.CreateIsometric(doc, viewFamilyType!.Id);
                            tx.Commit();
                        }
                    }

                    if (view == null)
                    {
                        failed++;
                        doc.Close(false);
                        continue;
                    }

                    string outNoExt = Path.Combine(Path.GetDirectoryName(cacheFile)!,
                                                   Path.GetFileNameWithoutExtension(cacheFile)!);
                    Directory.CreateDirectory(Path.GetDirectoryName(cacheFile)!);

                    var imgOpts = new ImageExportOptions
                    {
                        ExportRange = ExportRange.CurrentView,
                        ZoomType = ZoomFitType.FitToPage,
                        PixelSize = 256,
                        FitDirection = FitDirectionType.Horizontal,
                        HLRandWFViewsFileType = ImageFileType.PNG,
                        FilePath = outNoExt,
                        ShadowViewsFileType = ImageFileType.PNG
                    };

                    imgOpts.SetViewsAndSheets(new List<ElementId> { view.Id });
                    doc.ExportImage(imgOpts);

                    string exported = outNoExt + ".png";
                    if (File.Exists(exported))
                    {
                        if (File.Exists(cacheFile)) File.Delete(cacheFile);
                        File.Move(exported, cacheFile);
                        success++;
                    }
                    else failed++;

                    doc.Close(false);
                }
            }
            catch (Exception ex)
            {
                failed++;
                System.Diagnostics.Debug.WriteLine($"❌ {file}: {ex.Message}");
            }
        }

        // Sluit voortgangsvenster
        progressWindow?.Dispatcher.Invoke(() => progressWindow.Close());

        TaskDialog.Show("Thumbnails",
            $"✅ {success} opgeslagen\n❌ {failed} mislukt\n📁 {cacheDir}");
    }

    // ⚙️ Gebruik MD5-hash zodat oude cache-bestanden herkend worden
    private static string GetCacheFileName(string path)
    {
        using var md5 = MD5.Create();
        byte[] inputBytes = Encoding.UTF8.GetBytes(path.ToLowerInvariant());
        byte[] hashBytes = md5.ComputeHash(inputBytes);
        string hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        return $"{hash}.png";
    }
}