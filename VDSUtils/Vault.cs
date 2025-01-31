﻿using Autodesk.Connectivity.WebServices;
using Autodesk.Connectivity.WebServicesTools;
using Autodesk.DataManagement.Client.Framework.Vault.Currency.Connections;
using Autodesk.DataManagement.Client.Framework.Vault.Currency.Entities;
using Autodesk.DataManagement.Client.Framework.Vault.Currency.PersistentId;
using Autodesk.DataManagement.Client.Framework.Vault.Currency.Properties;
using Inventor;
using System;
using System.Collections.Generic;
using System.Linq;
using ACET = Autodesk.Connectivity.Explorer.ExtensibilityTools;
using AcInterop = Autodesk.AutoCAD.Interop;
using AcInteropCom = Autodesk.AutoCAD.Interop.Common;
using ACW = Autodesk.Connectivity.WebServices;
using VDF = Autodesk.DataManagement.Client.Framework;

namespace VdsSampleUtilities
{
    /// <summary>
    /// Class extending VDS Vault scripts
    /// </summary>
    public class VltHelpers

    {
        /// <summary>
        /// UserCredentials1 and UserCredentials2 differentiate overloads as powershell can't handle
        /// UserCredentials1 returns read-write loginuser object
        /// </summary>
        /// <param name="server">IP Address or DNS Name of ADMS Server</param>
        /// <param name="vault">Name of vault to connect to</param>
        /// <param name="user">User name</param>
        /// <param name="pw">Password</param>
        /// <returns>User Credentials</returns>
        public Autodesk.Connectivity.WebServicesTools.UserPasswordCredentials UserCredentials1(string server, string vault, string user, string pw)
        {
            ServerIdentities mServer = new ServerIdentities();
            mServer.DataServer = server;
            mServer.FileServer = server;
            Autodesk.Connectivity.WebServicesTools.UserPasswordCredentials mCred = new Autodesk.Connectivity.WebServicesTools.UserPasswordCredentials(mServer, vault, user, pw);
            return mCred;
        }

        /// <summary>
        /// UserCredentials1 and UserCredentials2 differentiate overloads as powershell can't handle
        /// UserCredentials2 returns readonly loginuser object
        /// </summary>
        /// <param name="server">IP Address or DNS Name of ADMS Server</param>
        /// <param name="vault">Name of vault to connect to</param>
        /// <param name="user">User name</param>
        /// <param name="pw">Password</param>
        /// <param name="rw">Set to "True" to allow Read/Write access</param>
        /// <returns></returns>
        public Autodesk.Connectivity.WebServicesTools.UserPasswordCredentials UserCredentials2(string server, string vault, string user, string pw, bool rw = true)
        {
            ServerIdentities mServer = new ServerIdentities();
            mServer.DataServer = server;
            mServer.FileServer = server;
            Autodesk.Connectivity.WebServicesTools.UserPasswordCredentials mCred = new Autodesk.Connectivity.WebServicesTools.UserPasswordCredentials(mServer, vault, user, pw, rw);
            return mCred;
        }


        private List<ACW.SrchCond> CreateSrchConds(VDF.Vault.Currency.Connections.Connection conn, Dictionary<string, string> SearchCriteria, bool MatchAllCriteria)
        {
            ACW.PropDef[] mFilePropDefs = conn.WebServiceManager.PropertyService.GetPropertyDefinitionsByEntityClassId("FILE");
            //iterate mSearchcriteria to get property definitions and build AWS search criteria
            List<ACW.SrchCond> mSrchConds = new List<ACW.SrchCond>();
            int i = 0;
            foreach (var item in SearchCriteria)
            {
                ACW.PropDef mFilePropDef = mFilePropDefs.Single(n => n.DispName == item.Key);
                ACW.SrchCond mSearchCond = new ACW.SrchCond();
                {
                    mSearchCond.PropDefId = mFilePropDef.Id;
                    mSearchCond.PropTyp = ACW.PropertySearchType.SingleProperty;
                    mSearchCond.SrchOper = 3; //equals
                    if (MatchAllCriteria) mSearchCond.SrchRule = ACW.SearchRuleType.Must;
                    else mSearchCond.SrchRule = ACW.SearchRuleType.May;
                    mSearchCond.SrchTxt = item.Value;
                }
                mSrchConds.Add(mSearchCond);
                i++;
            }
            return mSrchConds;
        }

        private VDF.Vault.Settings.AcquireFilesSettings CreateAcquireSettings(VDF.Vault.Currency.Connections.Connection conn, bool CheckOut)
        {
            VDF.Vault.Settings.AcquireFilesSettings settings = new VDF.Vault.Settings.AcquireFilesSettings(conn);
            if (CheckOut)
            {
                settings.DefaultAcquisitionOption = VDF.Vault.Settings.AcquireFilesSettings.AcquisitionOption.Checkout;
            }
            else
            {
                settings.DefaultAcquisitionOption = VDF.Vault.Settings.AcquireFilesSettings.AcquisitionOption.Download;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeChildren = true;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.RecurseChildren = true;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeAttachments = true;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeLibraryContents = true;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.ReleaseBiased = true;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.VersionGatheringOption = VDF.Vault.Currency.VersionGatheringOption.Revision;
                settings.OptionsRelationshipGathering.IncludeLinksSettings.IncludeLinks = false;
                VDF.Vault.Settings.AcquireFilesSettings.AcquireFileResolutionOptions mResOpt = new VDF.Vault.Settings.AcquireFilesSettings.AcquireFileResolutionOptions();
                mResOpt.OverwriteOption = VDF.Vault.Settings.AcquireFilesSettings.AcquireFileResolutionOptions.OverwriteOptions.ForceOverwriteAll;
                mResOpt.SyncWithRemoteSiteSetting = VDF.Vault.Settings.AcquireFilesSettings.SyncWithRemoteSite.Always;
            }

            return settings;
        }

        /// <summary>
        /// Deprecated - no longer required, as the overload is removed in 2017 API
        /// </summary>
        /// <param name="svc"></param>
        /// <param name="FldIds"></param>
        /// <param name="m_PropArray"></param>
        /// <returns></returns>
        public Boolean UpdateFolderProp2(WebServiceManager svc, long[] FldIds, PropInstParamArray[] m_PropArray)
        {
            try
            {
                svc.DocumentServiceExtensions.UpdateFolderProperties(FldIds, m_PropArray);
                return true;
            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        /// LinkManager.GetLinkedChildren has an override list; the input is of type IEntity. 
        /// This wrapper allows to input commonly known object types, like Ids and entity names instead.
        /// </summary>
        /// <param name="conn">The utility dll is not connected to Vault; 
        /// we need to leverage the established connection to call LinkManager methods</param>
        /// <param name="mId">The parent entity's id to get linked children of</param>
        /// <param name="mClsId">The parent entity's class name; allowed values are FILE FLDR and CUSTENT. 
        /// CO and ITEM cannot have linked children, as they use specific links to related child objects.</param>
        /// <param name="mFilter">Limit the search on links to a particular class; providing an empty value "" will result in a search on all types</param>
        /// <returns>List of entity Ids</returns>
        public List<long> mGetLinkedChildren1(Connection conn, long mId, string mClsId, string mFilter)
        {
            IEnumerable<PersistableIdEntInfo> mEntInfo = new PersistableIdEntInfo[] { new PersistableIdEntInfo(mClsId, mId, true, false) };
            IDictionary<PersistableIdEntInfo, IEntity> mIEnts = conn.EntityOperations.ConvertEntInfosToIEntities(mEntInfo);
            IEntity mIEnt = null;
            try
            {
                foreach (var item in mIEnts)
                {
                    mIEnt = item.Value;
                }
                IEnumerable<IEntity> mLinkedChldrn = conn.LinkManager.GetLinkedChildren(mIEnt, mFilter);
                //return mLinkedChldrn;
                List<long> mLinkedIds = new List<long>();
                foreach (var item in mLinkedChldrn)
                {
                    mLinkedIds.Add(item.EntityIterationId);
                }
                return mLinkedIds;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Evaluation of overload 2; see mGetLinkedchildren1 for detailed description
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="mParEntIds"></param>
        /// <param name="mClsIds"></param>
        /// <returns></returns>
        private IEnumerable<IEntity> GetLinkedChildren2(Connection conn, long[] mParEntIds, string[] mClsIds)
        {
            List<PersistableIdEntInfo> mEntInfo = new List<PersistableIdEntInfo>();
            for (int i = 0; i < mParEntIds.Length; i++)
            {
                mEntInfo.Add(new PersistableIdEntInfo("CUSTENT", mParEntIds[i], true, false));
            }
            //List<CustEnt> mEnts = new List<CustEnt>();
            //CustEnt mEnt = new CustEnt();
            //foreach (var item in mParentEnts)
            //{
            //    mEnt = (CustEnt)item;
            //    mEnts.Add(mEnt);
            //}
            //List<PersistableIdEntInfo> mEntInfo = new List<PersistableIdEntInfo>();
            //foreach (var item in mEnts)

            //{
            //    mEntInfo.Add( new PersistableIdEntInfo(mClsIds[0], item.Id, true, false));
            //}

            IDictionary<PersistableIdEntInfo, IEntity> mIEnts = conn.EntityOperations.ConvertEntInfosToIEntities(mEntInfo.AsEnumerable());
            List<IEntity> mIEnt = new List<IEntity>();
            try
            {
                foreach (var item in mIEnts)
                {
                    mIEnt.Add(item.Value);
                }
                IEnumerable<IEntity> mLinkedChldrn = conn.LinkManager.GetLinkedChildren(mIEnt.AsEnumerable(), mClsIds.AsEnumerable());
                return mLinkedChldrn;
            }
            catch
            {
                return null;
            }
        }

        private bool IsCadFile(System.IO.FileInfo FileInfo)
        {
            //don't add Inventor files except single part files
            List<string> mFileExtensions = new List<string> { ".iam", "ipn", ".idw", ".dwg" };
            if (mFileExtensions.Any(n => FileInfo.Extension == n))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Update file properties
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="mFile"></param>
        /// <param name="mPropDictonary"></param>
        /// <returns>True if updated successfully</returns>
        public bool mUpdateFileProperties(VDF.Vault.Currency.Connections.Connection conn,
            Autodesk.Connectivity.WebServices.File mFile, Dictionary<Autodesk.Connectivity.WebServices.PropDef, object> mPropDictonary)
        {
            try
            {
                ACET.IExplorerUtil mExplUtil = Autodesk.Connectivity.Explorer.ExtensibilityTools.ExplorerLoader.LoadExplorerUtil(
                                            conn.Server, conn.Vault, conn.UserID, conn.Ticket);

                mExplUtil.UpdateFileProperties(mFile, mPropDictonary);
                return true;
            }
            catch
            {
                return false;
            }

        }

        /// <summary>
        /// Update file properties
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="mFile"></param>
        /// <param name="mProps">Dictionary(string,string)</param>
        /// <returns>True if updated successfully</returns>
        public bool mUpdateFileProperties2(VDF.Vault.Currency.Connections.Connection conn,
            Autodesk.Connectivity.WebServices.File mFile, Dictionary<string, string> mProps)
        {
            var mPropDictonary = new Dictionary<Autodesk.Connectivity.WebServices.PropDef, object>();

            var propDefs = conn.WebServiceManager.PropertyService.GetPropertyDefinitionsByEntityClassId("FILE");

            if (propDefs.Length != 0)
            {
                foreach (KeyValuePair<string, string> prop in mProps)
                {
                    mPropDictonary.Add(propDefs.First(x => x.DispName == prop.Key), prop.Value);
                }

                try
                {
                    var UpdateFilePropertiesResulst = mUpdateFileProperties(conn, mFile, mPropDictonary);
                    return UpdateFilePropertiesResulst;
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Downloads Vault file using full file path, e.g. "$/Designs/Base.ipt". Returns full file name in local working folder (download enforces override, if local file exists),
        /// returns "FileNotFound if file does not exist at indicated location.
        /// Preset Options: Download Children (recursively) = Enabled, Enforce Overwrite = True
        /// </summary>
        /// <param name="conn">Current Vault Connection</param>
        /// <param name="VaultFullFileName">FullFilePath</param>
        /// <param name="CheckOut">Optional. File downloaded does NOT check-out as default.</param>
        /// <returns>Local path/filename or error statement "FileNotFound"</returns>
        public string mGetFileByFullFileName(VDF.Vault.Currency.Connections.Connection conn, string VaultFullFileName, bool CheckOut = false)
        {
            List<string> mFiles = new List<string>();
            mFiles.Add(VaultFullFileName);
            Autodesk.Connectivity.WebServices.File[] wsFiles = conn.WebServiceManager.DocumentService.FindLatestFilesByPaths(mFiles.ToArray());
            VDF.Vault.Currency.Entities.FileIteration mFileIt = new VDF.Vault.Currency.Entities.FileIteration(conn, (wsFiles[0]));

            VDF.Vault.Settings.AcquireFilesSettings settings = new VDF.Vault.Settings.AcquireFilesSettings(conn);
            if (CheckOut)
            {
                settings.DefaultAcquisitionOption = VDF.Vault.Settings.AcquireFilesSettings.AcquisitionOption.Checkout;
            }
            else
            {
                settings.DefaultAcquisitionOption = VDF.Vault.Settings.AcquireFilesSettings.AcquisitionOption.Download;
            }
            settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeChildren = true;
            settings.OptionsRelationshipGathering.FileRelationshipSettings.RecurseChildren = true;
            settings.OptionsRelationshipGathering.FileRelationshipSettings.VersionGatheringOption = VDF.Vault.Currency.VersionGatheringOption.Latest;
            settings.OptionsRelationshipGathering.IncludeLinksSettings.IncludeLinks = false;
            VDF.Vault.Settings.AcquireFilesSettings.AcquireFileResolutionOptions mResOpt = new VDF.Vault.Settings.AcquireFilesSettings.AcquireFileResolutionOptions();
            mResOpt.OverwriteOption = VDF.Vault.Settings.AcquireFilesSettings.AcquireFileResolutionOptions.OverwriteOptions.ForceOverwriteAll;
            mResOpt.SyncWithRemoteSiteSetting = VDF.Vault.Settings.AcquireFilesSettings.SyncWithRemoteSite.Always;
            settings.AddFileToAcquire(mFileIt, settings.DefaultAcquisitionOption);
            VDF.Vault.Results.AcquireFilesResults results = conn.FileManager.AcquireFiles(settings);
            if (results != null)
            {
                try
                {
                    VDF.Vault.Results.FileAcquisitionResult mFilesDownloaded = results.FileResults.Last();
                    return mFilesDownloaded.LocalPath.FullPath.ToString();
                }
                catch (Exception)
                {
                    return "FileFoundButDownloadFailed";
                }
            }
            return "FileNotFound";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conn">Current Vault Connection</param>
        /// <param name="LocalPath">File or Folder path in local working folder</param>
        /// <returns>Vault Folder Path; if LocalPath is a Filepath, the file's parent Folderpath returns</returns>
        public string ConvertLocalPathToVaultPath(VDF.Vault.Currency.Connections.Connection conn, string LocalPath)
        {
            string mVaultPath = null;
            string mWf = conn.WorkingFoldersManager.GetWorkingFolder("$/").FullPath;
            if (LocalPath.Contains(mWf))
            {
                if (IsFilePath(LocalPath) == true)
                {
                    System.IO.FileInfo fileInfo = new System.IO.FileInfo(LocalPath);
                    LocalPath = fileInfo.DirectoryName;
                }
                if (IsDirPath(LocalPath) == true)
                {
                    mVaultPath = LocalPath.Replace(mWf, "$/");
                    mVaultPath = mVaultPath.Replace("\\", "/");
                    return mVaultPath;
                }
                else
                {
                    return "Invalid local path";
                }
            }
            else
            {
                return "Error: Local path outside of working folder";
            }
        }

        private bool IsFilePath(string path)
        {
            if (System.IO.File.Exists(path)) return true;
            return false;
        }

        private bool IsDirPath(string path)
        {
            if (System.IO.Directory.Exists(path)) return true;
            return false;
        }

        /// <summary>
        /// Find 1 to many file(s) by 1 to many search criteria as property/value pairs. 
        /// Downloads first file matching all or any search criterias. 
        /// Preset Search Operator Options: [Property] is (exactly) [Value]; multiple conditions link up using AND condition.
        /// Preset Download Options: Download Children (recursively) = Enabled, Enforce Overwrite = True
        /// </summary>
        /// <param name="conn">Current Vault Connection</param>
        /// <param name="SearchCriteria">Dictionary of property/value pairs</param>
        /// <param name="MatchAllCriteria">Optional. Switches AND/OR conditions using multiple criterias. Default is true</param>
        /// <param name="CheckOut">Optional. File downloaded does NOT check-out as default</param>
        /// <param name="FoldersSearched">Optional. Limit search scope to given folder path(s).</param>
        /// <returns>Local path/filename</returns>
        public string GetFileBySearchCriteria(VDF.Vault.Currency.Connections.Connection conn, Dictionary<string, string> SearchCriteria, bool MatchAllCriteria = true, bool CheckOut = false, string[] FoldersSearched = null)
        {
            //FoldersSearched: Inventor files are expected in IPJ registered path's only. In case of null use these:
            ACW.Folder[] mFldr;
            List<long> mFolders = new List<long>();
            if (FoldersSearched != null)
            {
                mFldr = conn.WebServiceManager.DocumentService.FindFoldersByPaths(FoldersSearched);
                foreach (ACW.Folder folder in mFldr)
                {
                    if (folder.Id != -1) mFolders.Add(folder.Id);
                }
            }

            List<String> mFilesFound = new List<string>();
            List<String> mFilesDownloaded = new List<string>();
            //combine all search criteria
            List<ACW.SrchCond> mSrchConds = CreateSrchConds(conn, SearchCriteria, MatchAllCriteria);
            List<ACW.File> totalResults = new List<ACW.File>();
            string bookmark = string.Empty;
            ACW.SrchStatus status = null;

            while (status == null || totalResults.Count < status.TotalHits)
            {
                ACW.File[] mSrchResults = conn.WebServiceManager.DocumentService.FindFilesBySearchConditions(
                    mSrchConds.ToArray(), null, mFolders.ToArray(), true, true, ref bookmark, out status);
                if (mSrchResults != null) totalResults.AddRange(mSrchResults);
                else break;
            }
            //if results not empty
            if (totalResults.Count >= 1)
            {
                ACW.File wsFile = totalResults.First<ACW.File>();
                VDF.Vault.Currency.Entities.FileIteration mFileIt = new VDF.Vault.Currency.Entities.FileIteration(conn, (wsFile));

                //build download options including DefaultAcquisitionOptions
                VDF.Vault.Settings.AcquireFilesSettings settings = CreateAcquireSettings(conn, false);
                settings.AddFileToAcquire(mFileIt, settings.DefaultAcquisitionOption);

                //download
                VDF.Vault.Results.AcquireFilesResults results = conn.FileManager.AcquireFiles(settings);

                if (CheckOut)
                {
                    //define checkout options and checkout
                    settings = CreateAcquireSettings(conn, true);
                    settings.AddFileToAcquire(mFileIt, settings.DefaultAcquisitionOption);
                    results = conn.FileManager.AcquireFiles(settings);
                }

                //refine and validate output
                if (results != null)
                {
                    try
                    {
                        if (results.FileResults.Any(n => n.File.EntityName == mFileIt.EntityName))
                        {
                            mFilesDownloaded.Add(conn.WorkingFoldersManager.GetPathOfFileInWorkingFolder(mFileIt).FullPath.ToString());
                        }

                        return mFilesDownloaded[0];

                    }
                    catch (Exception)
                    {
                        return "CheckOut failed";
                    }
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return "File not found";
            }
        }


        /// <summary>
        /// Adds local file to Vault.
        /// </summary>
        /// <param name="conn">Current Vault Connection</param>
        /// <param name="FullFileName">File path and name of file to add in local working folder.</param>
        /// <param name="VaultFolderPath">Full path in Vault, e.g. "$/Designs/P-00000</param>
        /// <param name="UpdateExisting">Creates new file version if existing file is available for check-out.</param>
        /// <returns>Returns True/False on success/failure; returns false if the file exists and UpdateExisting = false. Returns false for IAM, IPN, IDW/DWG</returns>
        public bool AddFile(VDF.Vault.Currency.Connections.Connection conn, string FullFileName, string VaultFolderPath, bool UpdateExisting = true)
        {
            //exclude CAD files with references
            System.IO.FileInfo mLocalFileInfo = new System.IO.FileInfo(FullFileName);
            if (IsCadFile(mLocalFileInfo))
            {
                return false;
            }

            Autodesk.Connectivity.WebServicesTools.WebServiceManager mWsMgr = conn.WebServiceManager;

            ACW.Folder mFolder = mWsMgr.DocumentService.FindFoldersByPaths(new string[] { VaultFolderPath }).FirstOrDefault();
            if (mFolder.Id == -1)
            {
                return false;
            }
            string vaultFilePath = System.IO.Path.Combine(mFolder.FullName, mLocalFileInfo.Name).Replace("\\", "/");

            ACW.File wsFile = mWsMgr.DocumentService.FindLatestFilesByPaths(new string[] { vaultFilePath }).First();

            VDF.Currency.FilePathAbsolute vdfPath = new VDF.Currency.FilePathAbsolute(mLocalFileInfo.FullName);
            VDF.Vault.Currency.Entities.FileIteration vdfFile = null;
            VDF.Vault.Currency.Entities.FileIteration addedFile = null;
            VDF.Vault.Currency.Entities.FileIteration mUploadedFile = null;
            if (wsFile == null || wsFile.Id < 0)
            {
                // add new file to Vault
                var folderEntity = new Autodesk.DataManagement.Client.Framework.Vault.Currency.Entities.Folder(conn, mFolder);
                try
                {
                    addedFile = conn.FileManager.AddFile(folderEntity, "generated file", null, null, ACW.FileClassification.None, false, vdfPath);
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                if (UpdateExisting == true)
                {
                    // checkin new file version
                    VDF.Vault.Settings.AcquireFilesSettings aqSettings = new VDF.Vault.Settings.AcquireFilesSettings(conn)
                    {
                        DefaultAcquisitionOption = VDF.Vault.Settings.AcquireFilesSettings.AcquisitionOption.Checkout
                    };
                    vdfFile = new VDF.Vault.Currency.Entities.FileIteration(conn, wsFile);
                    aqSettings.AddEntityToAcquire(vdfFile);
                    var results = conn.FileManager.AcquireFiles(aqSettings);
                    try
                    {
                        mUploadedFile = conn.FileManager.CheckinFile(results.FileResults.First().File, "auto-updated file", false, null, null, false, null, ACW.FileClassification.None, false, vdfPath);
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Get the local file's status in Vault.
        /// Validate the ErrorState = "None" to get all return values for vaulted files.
        /// Validate the ErrorState = (LocalFileNotFoundVaultFileNotFound|VaultFileNotFound) to validate files before first time check-in
        /// </summary>
        /// <param name="conn">Current Vault Connection</param>
        /// <param name="LocalFullFileName">Local path and file name, e.g., ThisDoc.FullFileName</param>
        /// <returns>ErrorState only if file is not added to Vault yet; otherwise Vault's default file status enumerations of CheckOutState, ConsumableState, ErrorState, LocalEditsState, LockState, RevisionState, VersionState</returns>
        public Dictionary<string, string> GetVaultFileStatus(VDF.Vault.Currency.Connections.Connection conn, string LocalFullFileName)
        {
            Dictionary<string, string> keyValues = new Dictionary<string, string>();

            //convert the local path to the corresponding Vault path; note - the file might be a virtual (to be created in future) one
            System.IO.FileInfo fileInfo = new System.IO.FileInfo(LocalFullFileName);
            string mVltFullFileName = null;
            string mWf = conn.WorkingFoldersManager.GetWorkingFolder("$/").FullPath;
            if (LocalFullFileName.Contains(mWf))
                mVltFullFileName = LocalFullFileName.Replace(mWf, "$/");
            mVltFullFileName = mVltFullFileName.Replace("\\", "/");

            //get the file object consuming the Vault Path; if the file does not exist return the VaultFileNotFound status information; it is a custom ErrorState info
            Autodesk.Connectivity.WebServicesTools.WebServiceManager mWsMgr = conn.WebServiceManager;
            ACW.File mFile = mWsMgr.DocumentService.FindLatestFilesByPaths(new string[] { mVltFullFileName }).FirstOrDefault();

            if (mFile.Id == -1) // file not found locally and in Vault
            {
                if (!fileInfo.Exists)
                {
                    keyValues.Add("ErrorState", "LocalFileNotFoundVaultFileNotFound");
                }
                else
                {
                    keyValues.Add("ErrorState", "VaultFileNotFound");
                }
                return keyValues;
            }

            VDF.Vault.Currency.Entities.FileIteration mFileIteration = new VDF.Vault.Currency.Entities.FileIteration(conn, mFile);

            PropertyDefinitionDictionary mProps = conn.PropertyManager.GetPropertyDefinitions(VDF.Vault.Currency.Entities.EntityClassIds.Files, null, PropertyDefinitionFilter.IncludeAll);

            PropertyDefinition mVaultStatus = mProps[PropertyDefinitionIds.Client.VaultStatus];

            EntityStatusImageInfo status = conn.PropertyManager.GetPropertyValue(mFileIteration, mVaultStatus, null) as EntityStatusImageInfo;
            keyValues.Add("FileName", mFile.Name);
            keyValues.Add("FullFileName", (LocalFullFileName));
            keyValues.Add("CheckOut", mFile.CheckedOut.ToString());
            keyValues.Add("EditedBy", mFile.CreateUserName);
            keyValues.Add("CheckOutPC", mFile.CkOutMach);
            keyValues.Add("CheckOutState", status.Status.CheckoutState.ToString());
            keyValues.Add("ConsumableState", status.Status.ConsumableState.ToString());
            keyValues.Add("ErrorState", status.Status.ErrorState.ToString());
            keyValues.Add("LocalEditsState", status.Status.LocalEditsState.ToString());
            keyValues.Add("LockState", status.Status.LockState.ToString());
            keyValues.Add("RevisionState", status.Status.RevisionState.ToString());
            keyValues.Add("VersionState", status.Status.VersionState.ToString());

            return keyValues;
        }
    }


    /// <summary>
    /// Class sharing options to interact with hosting Inventor session
    /// </summary>
    public class InvHelpers
    {
        Inventor.Application m_Inv = null;
        Inventor.Document m_Doc = null;
        Inventor.DrawingDocument m_DrawDoc = null;
        Inventor.PresentationDocument m_IpnDoc = null;
        String m_ModelPath = null;
        Inventor.CommandManager m_InvCmdMgr = null;

        [System.Runtime.InteropServices.DllImport("User32.dll", SetLastError = true)]
        static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);

        /// <summary>
        /// Retrieve property value of main view referenced model
        /// </summary>
        /// <param name="m_InvApp">Connect to the hosting instance of the VDS dialog</param>
        /// <param name="m_ViewModelFullName"></param>
        /// <param name="m_PropName">Display Name</param>
        /// <returns></returns>
        public object m_GetMainViewModelPropValue(object m_InvApp, String m_ViewModelFullName, String m_PropName)
        {
            try
            {
                m_Inv = (Inventor.Application)m_InvApp;
                m_Doc = m_Inv.Documents.Open(m_ViewModelFullName, false);
                foreach (PropertySet m_PropSet in m_Doc.PropertySets)
                {
                    foreach (Property m_Prop in m_PropSet)
                    {
                        if (m_Prop.Name == m_PropName)
                        {
                            return m_Prop.Value;
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return null;
        }

        /// <summary>
        /// Gets the 3D model (ipt/iam/ipn) linked to the main view of the current (active) drawing.
        /// Gets the 3D model (iam) linked to the main view of the current (active) presentation.
        /// </summary>
        /// <param name="m_InvApp">Running host (instance of Inventor) of calling VDS Dialog.</param>
        /// <returns>Returns the fullfilename (path and filename incl. extension) of the referenced model as string.</returns>
        public String m_GetMainViewModelPath(object m_InvApp)
        {
            try
            {
                m_Inv = (Inventor.Application)m_InvApp;

                if (m_Inv.ActiveDocumentType == DocumentTypeEnum.kDrawingDocumentObject)
                {
                    m_DrawDoc = (DrawingDocument)m_Inv.ActiveDocument;
                    Sheet m_Sheet = m_DrawDoc.ActiveSheet;
                    DrawingView m_DrwView = m_Sheet.DrawingViews[1];
                    if (!(m_DrwView is null))
                    {
                        m_ModelPath = m_DrwView.ReferencedFile.FullFileName;
                        return m_ModelPath;
                    }
                }

                if (m_Inv.ActiveDocumentType == DocumentTypeEnum.kPresentationDocumentObject)
                {
                    m_IpnDoc = (PresentationDocument)m_Inv.ActiveDocument;
                    if (m_IpnDoc.ReferencedDocuments.Count >= 1)
                    {
                        m_ModelPath = m_IpnDoc.ReferencedDocuments[1].FullDocumentName;
                        return m_ModelPath;
                    }
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Delete orphaned drawing sheets. Sheet format consuming workflows likely cause an unused sheet1
        /// </summary>
        /// <param name="m_InvApp">Inventor Application ($Application)</param>
        /// <returns>false on unhandled errors, else true</returns>
        public bool m_RemoveOrphanedSheets(object m_InvApp)
        {
            try
            {
                m_Inv = (Inventor.Application)m_InvApp;

                if (m_Inv.ActiveDocumentType == DocumentTypeEnum.kDrawingDocumentObject)
                {
                    m_DrawDoc = (DrawingDocument)m_Inv.ActiveDocument;
                    foreach (Sheet sheet in m_DrawDoc.Sheets)
                    {
                        if (sheet.DrawingViews.Count == 0 && sheet != m_DrawDoc.ActiveSheet)
                        {
                            sheet.Delete(false);
                        }
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Return running Inventor application
        /// </summary>
        /// <returns></returns>
        public Inventor.Application m_InventorApplication()
        {
            // Try to get an active instance of Inventor
            try
            {
                return System.Runtime.InteropServices.Marshal.GetActiveObject("Inventor.Application") as Inventor.Application;
            }
            catch
            {
                return null;
            }
        }


        /// <summary>
        /// Return active Inventor document
        /// </summary>
        /// <param name="m_InvApp">Inventor Application ($Application)</param>
        /// <returns></returns>
        public string m_ActiveDocFullFileName(object m_InvApp)
        {
            m_Inv = (Inventor.Application)m_InvApp;
            if (m_Inv.ActiveDocument != null)
            {
                return m_Inv.ActiveDocument.FullFileName;
            }
            else
            {
                return null;
            }

        }


        /// <summary>
        /// Place component in active Inventor assembly document; deprecated: VDS includes 'Insert to CAD' as a default.
        /// </summary>
        /// <param name="m_InvApp"></param>
        /// <param name="m_CompFullFileName"></param>
        public void m_PlaceComponent(object m_InvApp, String m_CompFullFileName)
        {
            m_Inv = (Inventor.Application)m_InvApp;
            if (m_Inv.ActiveDocumentType == DocumentTypeEnum.kAssemblyDocumentObject)
            {
                try
                {
                    m_InvCmdMgr = m_Inv.CommandManager;
                    m_InvCmdMgr.PostPrivateEvent(PrivateEventTypeEnum.kFileNameEvent, m_CompFullFileName);
                    Inventor.ControlDefinition m_InvCtrlDef = (ControlDefinition)m_InvCmdMgr.ControlDefinitions["AssemblyPlaceComponentCmd"];
                    //bring Inventor to front
                    IntPtr mWinPt = (IntPtr)m_Inv.MainFrameHWND;
                    SwitchToThisWindow(mWinPt, true);
                    m_InvCtrlDef.Execute();
                }
                catch
                {

                }
            }
        }


        /// <summary>
        /// validate active Factory Design Utility AddIn
        /// </summary>
        /// <param name="mInvApp">Inventor Application ($Application)</param>
        /// <returns></returns>
        public bool m_FDUActive(object mInvApp)
        {
            m_Inv = (Application)mInvApp;
            try
            {
                ApplicationAddIn mFDUAddIn = m_Inv.ApplicationAddIns.get_ItemById("{031C8B05-13C0-4C6C-B8FD-5A19DACCB64F}");
                if (mFDUAddIn != null)
                {
                    if (mFDUAddIn.Activated)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Return FDU key/value pairs to identify Factory Layout or Factory Asset files
        /// </summary>
        /// <param name="m_InvApp">Inventor Application ($Application)</param>
        /// <param name="mFdsKeys">empty dictonary</param>
        /// <returns></returns>
        public Dictionary<string, string> m_GetFdsKeys(object m_InvApp, Dictionary<string, string> mFdsKeys)
        {
            try
            {
                m_Inv = (Inventor.Application)m_InvApp;
                m_Doc = m_Inv.ActiveDocument;
                if (m_Doc.DocumentInterests.HasInterest("factory.filetype.factory_layout_template"))
                {
                    //FDS Type
                    mFdsKeys.Add("FdsType", "FDS-Layout");

                    //FDS Property Set exists for syncronized layouts
                    foreach (PropertySet m_PropSet in m_Doc.PropertySets)
                    {
                        if (m_PropSet.Name == "autodesk.factory.inventor.DwgInv")
                        {
                            foreach (Property m_Prop in m_PropSet)
                            {
                                mFdsKeys.Add(m_Prop.Name, m_Prop.Value);
                            }
                            //Get Fullname set by synchronization, to avoid save to other location
                            mFdsKeys.Add("FdsNewFullFileName", m_Doc.File.FullFileName);
                            System.IO.FileInfo mFdsFileInfo = new System.IO.FileInfo(m_Doc.File.FullFileName);
                            string mFdsPath = mFdsFileInfo.Directory.FullName;
                            mFdsKeys.Add("FdsNewPath", mFdsPath);
                        }
                    }
                }
                if (m_Doc.DocumentInterests.HasInterest("factory.filetype.factory_asset"))
                {
                    mFdsKeys.Add("FdsType", "FDS-Asset");
                }
            }
            catch (Exception)
            {
                throw;
            }
            return mFdsKeys;
        }

        /// <summary>
        /// Return custom iPropertyset for AutoCAD files handled by Inventor FDU
        /// </summary>
        /// <param name="m_InvApp">Inventor Application ($Application)</param>
        /// <param name="mFdsKeys">empty Dictonary of String, String</param>
        /// <returns></returns>
        public Dictionary<string, string> m_GetFdsAcadProps(object m_InvApp, Dictionary<string, string> mFdsKeys)
        {
            Inventor.Document mDwgSource = null;
            DefaultNonInventorDWGFileOpenBehaviorEnum mUserOpenOpt = DefaultNonInventorDWGFileOpenBehaviorEnum.kRegularOpenNonInventorDWGFile;

            try
            {
                m_Inv = (Inventor.Application)m_InvApp;
                m_Doc = m_Inv.ActiveDocument;
                if (m_Doc.DocumentInterests.HasInterest("factory.filetype.factory_layout_template"))
                {
                    //FDS Type
                    mFdsKeys.Add("FdsType", "FDS-Layout");

                    //FDS Property Set exists for syncronized layouts
                    foreach (PropertySet m_PropSet in m_Doc.PropertySets)
                    {
                        if (m_PropSet.Name == "autodesk.factory.inventor.DwgInv")
                        {
                            foreach (Property m_Prop in m_PropSet)
                            {
                                mFdsKeys.Add(m_Prop.Name, m_Prop.Value);
                            }

                            //Get Fullname set by synchronization, to avoid save to other location
                            mFdsKeys.Add("FdsNewFullFileName", m_Doc.File.FullFileName);
                            System.IO.FileInfo mFdsFileInfo = new System.IO.FileInfo(m_Doc.File.FullFileName);
                            string mFdsPath = mFdsFileInfo.Directory.FullName;
                            mFdsKeys.Add("FdsNewPath", mFdsPath);

                            if (m_Doc.FileSaveCounter >= 0) //if save counter = 0, the file is currently in the sync process; we must not open the sync source then.
                            {
                                //Open the source DWG to read properties;
                                try
                                {
                                    string mFdsSourceFullFileName = mFdsPath + "\\" + mFdsKeys["DwgFileName"];
                                    //read inventor application option to reset later
                                    mUserOpenOpt = m_Inv.DrawingOptions.DefaultNonInventorDWGFileOpenBehavior;
                                    m_Inv.DrawingOptions.DefaultNonInventorDWGFileOpenBehavior = DefaultNonInventorDWGFileOpenBehaviorEnum.kRegularOpenNonInventorDWGFile;
                                    mDwgSource = m_Inv.Documents.Open(mFdsSourceFullFileName, false);
                                    //Read the properties and add to dictionary if a value exists
                                    foreach (PropertySet m_TempPropSet in mDwgSource.PropertySets)
                                    {
                                        if (m_TempPropSet.DisplayName.Contains("Summary") || m_TempPropSet.DisplayName == "User Defined Properties")
                                        {
                                            foreach (Property m_TempProp in m_TempPropSet)
                                            {
                                                if (!string.IsNullOrEmpty(m_TempProp.Value))
                                                {
                                                    mFdsKeys.Add(m_TempProp.Name, m_TempProp.Value);
                                                }
                                            }
                                        }
                                    }

                                }
                                catch (Exception)
                                {
                                    //throw;
                                }
                                finally
                                {
                                    mDwgSource.Close(true);
                                    //reset application option
                                    m_Inv.DrawingOptions.DefaultNonInventorDWGFileOpenBehavior = mUserOpenOpt;
                                }
                            }
                            else
                            {
                                mFdsKeys.Add("FdsAcadProps", "We can't retrieve properties before the calling file is saved.");
                            }
                        }
                    }
                }
                if (m_Doc.DocumentInterests.HasInterest("factory.filetype.factory_asset"))
                {
                    mFdsKeys.Add("FdsType", "FDS-Asset");
                }
            }
            catch (Exception)
            {
                throw;
            }
            return mFdsKeys;
        }

    }


    /// <summary>
    /// /// Class sharing options to interact with hosting AutoCAD session
    /// </summary>
    public class AcadHelpers
    {
        AcInterop.AcadApplication mAcad = null;
        private const string progID = "AutoCAD.Application";
        AcInterop.AcadDocument mAcDoc = null;

        [System.Runtime.InteropServices.DllImport("User32.dll", SetLastError = true)]
        static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);

        /// <summary>
        /// Get AutoCAD session hosting; deprecated as VDS >2017 dialogs share the hosting application object
        /// </summary>
        /// <returns></returns>
        private Boolean m_ConnectAcad()
        {
            try
            {
                mAcad = (AcInterop.AcadApplication)System.Runtime.InteropServices.Marshal.GetActiveObject(progID);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check for FDS Blocks in AutoCAD drawings
        /// </summary>
        /// <param name="m_AcadApp">AutoCAD Application ($Application)</param>
        /// <returns>True for Blocknames containing "FDS"</returns>
        public Boolean mFdsDrawing(object m_AcadApp)
        {
            mAcad = (AcInterop.AcadApplication)m_AcadApp;
            mAcDoc = mAcad.ActiveDocument;
            AcInteropCom.AcadDatabase m_AcDB = (dynamic)mAcDoc.Database;
            AcInteropCom.AcadSummaryInfo m_AcSummInfo = m_AcDB.SummaryInfo;
            foreach (AcInteropCom.AcadBlock mBlock in mAcDoc.Blocks)
            {
                if (mBlock.Name.Contains("FDS"))
                {
                    return true;
                };
            }
            return false;
        }


        private Boolean mFdsDict(object m_AcadApp)
        {
            mAcad = (AcInterop.AcadApplication)m_AcadApp;
            mAcDoc = mAcad.ActiveDocument;
            AcInteropCom.AcadDatabase m_AcDB = mAcDoc.Database;

            return false;
        }


        /// <summary>
        /// Switch running AutoCAD application
        /// </summary>
        /// <param name="m_AcadApp">AutoCAD Application ($Application)</param>
        private void m_GoToAcad(object m_AcadApp)
        {
            try
            {
                mAcad = (AcInterop.AcadApplication)m_AcadApp;
                mAcDoc = mAcad.ActiveDocument;
                IntPtr mWinPt = (IntPtr)mAcad.HWND;
                SwitchToThisWindow(mWinPt, true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

        }
    }
}

