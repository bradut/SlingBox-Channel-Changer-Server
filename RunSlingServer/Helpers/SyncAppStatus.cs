using Application.Abstractions;
using Domain.Abstractions;
using Domain.Helpers;
using Domain.Models;
using RunSlingServer.Configuration.Models;
using RunSlingServer.Configuration.Services;

namespace RunSlingServer.Helpers
{
    /// <summary>
    /// Update or create the SlingBoxServerStatus file with values
    /// from SlingerConfiguration and AppConfiguration
    /// </summary>
    public class SyncAppStatus
    {
        public static IResult<SlingBoxServerStatus> SynchronizeAppStatus(
            IFileSystemAccess fileSystemAccess,
            ISlingerConfigurationParser slingerConfigParser,
            in IAppConfiguration appConfig)
        {
            Console.WriteLine("SyncStatusWithSlingConfig"); //ToDo: Log

            var result = new Result<SlingBoxServerStatus>();

            var slingerConfiguration = slingerConfigParser.Parse();

            if (!slingerConfiguration.IsUnifiedConfig)
            {
                var errMsg = $"Please update your file '{slingerConfigParser.ConfigFilePath.ToUpper()}' to match the structure of 'UNIFIED_CONFIG.INI'\n" +
                             $" and try again.";
                result.AddErrorMessage(errMsg);

                return result;
            }

            var serverStatus = fileSystemAccess.LoadSlingBoxServerStatusFromFile() ??
                                       new SlingBoxServerStatus
                                       {
                                           SlingRemoteControlServiceUrl = appConfig.SlingRemoteControlServiceUrl,
                                           TvGuideUrl = appConfig.TvGuideUrl
                                       };
            var hasChangedRecords = SyncSlingBoxRecordings(slingerConfiguration, serverStatus);
            var hasChangedSlingBoxes = SyncSlingBoxProperties(slingerConfiguration, serverStatus);
            var hasChangedServerVars = SetServerWideVars(slingerConfiguration, serverStatus, appConfig);

            var hasChanges = hasChangedRecords || hasChangedSlingBoxes || hasChangedServerVars;

            SaveSlingBoxServerStatus(fileSystemAccess, serverStatus, hasChanges);

            result.Value = serverStatus;

            return result;
        }

        private static bool SyncSlingBoxRecordings(SlingerConfiguration slingerConfig, SlingBoxServerStatus slingBoxServerStatus)
        {
            var slingBoxNamesInSlingerConfig = slingerConfig.SlingBoxes.Select(skv => skv.Key).ToArray();
            var slingBoxNamesInSlingServerStatus = slingBoxServerStatus.SlingBoxes.Select(skv => skv.Key).ToArray();

            var slingBoxNamesToAddToServerStatus = slingBoxNamesInSlingerConfig.Except(slingBoxNamesInSlingServerStatus).ToArray();
            var slingBoxNamesToRemoveFromServerStatus = slingBoxNamesInSlingServerStatus.Except(slingBoxNamesInSlingerConfig).ToArray();

            foreach (var slingBoxName in slingBoxNamesToAddToServerStatus)
            {
                var slingBoxData = slingerConfig.SlingBoxes[slingBoxName];
                slingBoxServerStatus.AddSlingBox(slingBoxName, slingBoxData.SlingBoxId);
            }

            foreach (var slingBoxName in slingBoxNamesToRemoveFromServerStatus)
            {
                slingBoxServerStatus.RemoveSlingBox(slingBoxName);
            }

            var hasChanges = slingBoxNamesToAddToServerStatus.Any() || slingBoxNamesToRemoveFromServerStatus.Any();

            return hasChanges;
        }


        private static bool SyncSlingBoxProperties(SlingerConfiguration slingerConfig, SlingBoxServerStatus slingBoxServerStatus)
        {
            var hasChanges = false;

            var slingBoxesInSlingerConfig = slingerConfig.SlingBoxes;
 
            foreach (var (slingBoxName, slingBoxSlingerData) in slingBoxesInSlingerConfig)
            {
                var slingBoxStatus = slingBoxServerStatus.GetSlingBoxStatus(slingBoxName);

                if (slingBoxStatus.IsAnalogue != slingBoxSlingerData.IsAnalogue)
                {
                    slingBoxServerStatus.SetSlingBoxIsAnalogue(slingBoxName, slingBoxSlingerData.IsAnalogue);
                    hasChanges = hasChanges || true;
                }

                if (slingBoxStatus.TvGuideUrl != slingBoxSlingerData.TvGuideUrl)
                {
                    slingBoxServerStatus.SetSlingBoxTvGuideUrl(slingBoxName, slingBoxSlingerData.TvGuideUrl);
                    hasChanges = hasChanges || true;
                }
            }
            
            return hasChanges;
        }


        private static bool SetServerWideVars(SlingerConfiguration slingBoxServerConfiguration,
                                                      SlingBoxServerStatus slingBoxServerStatus,
                                                      IAppConfiguration appConfig)
        {
            var hasChanges = false;

            if (slingBoxServerStatus.UrlBase != slingBoxServerConfiguration.UrlBase)
            {
                slingBoxServerStatus.UrlBase = slingBoxServerConfiguration.UrlBase;
                hasChanges = true;
            }

            if (slingBoxServerStatus.TvGuideUrl != appConfig.TvGuideUrl)
            {
                slingBoxServerStatus.TvGuideUrl = appConfig.TvGuideUrl;
                hasChanges = hasChanges || true;
            }

            return hasChanges;
        }
        
        private static void SaveSlingBoxServerStatus(IFileSystemAccess fileSystemAccess,
                                                     ISerializeToJsonFile slingBoxServerStatus, 
                                                     bool hasChanges)
        {
            if (!hasChanges)
                return;

            fileSystemAccess.SaveToJsonFile(slingBoxServerStatus);
            Console.WriteLine($"File {slingBoxServerStatus.JsonFileName} has been updated");
        }

    }
}
