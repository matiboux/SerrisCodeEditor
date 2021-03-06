﻿using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using SerrisModulesServer.Items;
using SerrisModulesServer.Type;
using SerrisModulesServer.Type.Theme;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Core;

namespace SerrisModulesServer.Manager
{
    public static class ModulesWriteManager
    {
        public static Task<bool> AddModuleAsync(StorageFile module_zip)
        {
            ModulesDataCache.LoadModulesData();

            return Task.Run(async () => 
            {
                string id = Guid.NewGuid().ToString();
                using (ZipArchive zip_content = ZipFile.OpenRead(module_zip.Path))
                {
                    ZipArchiveEntry InfosJson = zip_content.GetEntry("infos.json");

                    if (InfosJson != null)
                    {
                        using (var reader = new StreamReader(InfosJson.Open()))
                        using (JsonReader JsonReader = new JsonTextReader(reader))
                        {
                            InfosModule module = new JsonSerializer().Deserialize<InfosModule>(JsonReader);
                            if (module != null)
                            {
                                id = module.ID;
                            }
                        }
                    }

                }

                StorageFolder folder_addon = await ModulesDataCache.ModulesListFolder.CreateFolderAsync(id + "", CreationCollisionOption.OpenIfExists);

                ZipFile.ExtractToDirectory(module_zip.Path, folder_addon.Path);

                StorageFile file_infos = await folder_addon.CreateFileAsync("infos.json", CreationCollisionOption.OpenIfExists);
                using (var reader = new StreamReader(await file_infos.OpenStreamForReadAsync()))
                using (JsonReader JsonReader = new JsonTextReader(reader))
                {
                    try
                    {
                        InfosModule content = new JsonSerializer().Deserialize<InfosModule>(JsonReader);

                        if (content != null)
                        {
                            content.ID = id; content.ModuleSystem = false; content.IsEnabled = true;

                            if (await folder_addon.TryGetItemAsync("theme_ace.js") != null)
                            {
                                content.ContainMonacoTheme = true;
                            }
                            else
                            {
                                content.ContainMonacoTheme = false;
                            }

                            switch (content.ModuleType)
                            {
                                case ModuleTypesList.Addon:
                                    content.CanBePinnedToToolBar = true;
                                    break;

                                case ModuleTypesList.Theme:
                                    content.CanBePinnedToToolBar = false;
                                    break;

                                case ModuleTypesList.ProgrammingLanguage:
                                    content.CanBePinnedToToolBar = false;
                                    break;
                            }

                            ModulesDataCache.ModulesListDeserialized.Modules.Add(content);
                            ModulesDataCache.WriteModulesListContentFile();

                            foreach (CoreApplicationView view in CoreApplication.Views)
                            {
                                await view.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                                {
                                    Messenger.Default.Send(new SMSNotification { Type = TypeUpdateModule.NewModule, ID = id });
                                });
                            }

                            return true;
                        }
                    }
                    catch
                    {
                        return false;
                    }
                }

                return true;

            });

        }

        public static async Task<bool> DeleteModuleViaIDAsync(string id)
        {
            ModulesDataCache.LoadModulesData();

            try
            {
                StorageFolder folder_module = await ModulesDataCache.ModulesListFolder.GetFolderAsync(id + "");
                await folder_module.DeleteAsync();
                ModulesDataCache.ModulesListDeserialized.Modules.Remove(ModulesDataCache.ModulesListDeserialized.Modules.First(m => m.ID == id));
                ModulesDataCache.WriteModulesListContentFile();

                foreach (CoreApplicationView view in CoreApplication.Views)
                {
                    await view.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        Messenger.Default.Send(new SMSNotification { Type = TypeUpdateModule.ModuleDeleted, ID = id });
                    });
                }

                return true;
            }
            catch
            {
                return false;
            }

        }

        public static async Task<bool> PushUpdateModuleAsync(InfosModule module)
        {
            ModulesDataCache.LoadModulesData();

            try
            {
                InfosModule _module = ModulesDataCache.ModulesListDeserialized.Modules.First(m => m.ID == module.ID);
                int index_module = ModulesDataCache.ModulesListDeserialized.Modules.IndexOf(_module);

                ModulesDataCache.ModulesListDeserialized.Modules[index_module] = module;

                ModulesDataCache.WriteModulesListContentFile();

                foreach (CoreApplicationView view in CoreApplication.Views)
                {
                    await view.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        Messenger.Default.Send(new SMSNotification { Type = TypeUpdateModule.UpdateModule, ID = module.ID });
                    });
                }
                return true;
            }
            catch
            {
                return false;
            }


        }

        public static async Task<bool> SetCurrentThemeIDAsync(string id, bool SendNotification)
        {
            ModulesDataCache.LoadModulesData();

            try
            {
                ModulesDataCache.ModulesListDeserialized.CurrentThemeID = id;
                ModulesDataCache.WriteModulesListContentFile();

                if (SendNotification)
                {
                    foreach (CoreApplicationView view in CoreApplication.Views)
                    {
                        await view.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            Messenger.Default.Send(new SMSNotification { Type = TypeUpdateModule.CurrentThemeUpdated, ID = id });
                        });
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }

        }

        public static async Task<bool> SetCurrentMonacoThemeIDAsync(string id, bool SendNotification)
        {
            ModulesDataCache.LoadModulesData();

            try
            {
                ModulesDataCache.ModulesListDeserialized.CurrentThemeMonacoID = id;
                ModulesDataCache.WriteModulesListContentFile();

                if (SendNotification)
                {
                    foreach (CoreApplicationView view in CoreApplication.Views)
                    {
                        await view.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            Messenger.Default.Send(new SMSNotification { Type = TypeUpdateModule.CurrentThemeUpdated, ID = id });
                        });
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }

        }

        public static async Task<bool> SetCurrentThemeAceEditorTempContentAsync()
        {
            ModulesDataCache.LoadModulesData();

            try
            {
                InfosModule module = ModulesAccessManager.GetModuleViaID(ModulesAccessManager.GetCurrentThemeMonacoID());

                StorageFile file_content = await ApplicationData.Current.LocalFolder.CreateFileAsync("themeace_temp.js", CreationCollisionOption.OpenIfExists);
                await FileIO.WriteTextAsync(file_content, await new ThemeReader(module.ID).GetThemeJSContentAsync());

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> SetCurrentThemeTempContentAsync()
        {
            StorageFolder folder_module = await ModulesDataCache.ModulesListFolder.CreateFolderAsync(ModulesAccessManager.GetCurrentThemeMonacoID() + "", CreationCollisionOption.OpenIfExists);
            StorageFile file_content_temp = await ApplicationData.Current.LocalFolder.CreateFileAsync("theme_temp.json", CreationCollisionOption.OpenIfExists), file_content = await folder_module.GetFileAsync("theme_ace.js");

            try
            {
                using (var reader = new StreamReader(await file_content.OpenStreamForReadAsync()))
                {
                    await FileIO.WriteTextAsync(file_content_temp, await reader.ReadToEndAsync());
                    return true;
                }
            }
            catch
            {
                return false;
            }

        }
    }

    public static class AsyncHelpers
    {
        /// <summary>
        /// Execute's an async Task<T> method which has a void return value synchronously
        /// </summary>
        /// <param name="task">Task<T> method to execute</param>
        public static void RunSync(Func<Task> task)
        {
            SynchronizationContext oldContext = SynchronizationContext.Current;
            var synch = new ExclusiveSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(synch);
            synch.Post(async _ =>
            {
                try
                {
                    await task();
                }
                catch (Exception e)
                {
                    synch.InnerException = e;
                    throw;
                }
                finally
                {
                    synch.EndMessageLoop();
                }
            }, null);
            synch.BeginMessageLoop();

            SynchronizationContext.SetSynchronizationContext(oldContext);
        }

        /// <summary>
        /// Execute's an async Task<T> method which has a T return type synchronously
        /// </summary>
        /// <typeparam name="T">Return Type</typeparam>
        /// <param name="task">Task<T> method to execute</param>
        /// <returns></returns>
        public static T RunSync<T>(Func<Task<T>> task)
        {
            SynchronizationContext oldContext = SynchronizationContext.Current;
            var synch = new ExclusiveSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(synch);
            var ret = default(T);
            synch.Post(async _ =>
            {
                try
                {
                    ret = await task();
                }
                catch (Exception e)
                {
                    synch.InnerException = e;
                    throw;
                }
                finally
                {
                    synch.EndMessageLoop();
                }
            }, null);
            synch.BeginMessageLoop();
            SynchronizationContext.SetSynchronizationContext(oldContext);
            return ret;
        }

        private class ExclusiveSynchronizationContext : SynchronizationContext
        {
            private bool done;
            public Exception InnerException { get; set; }
            readonly AutoResetEvent workItemsWaiting = new AutoResetEvent(false);
            readonly Queue<Tuple<SendOrPostCallback, object>> items = new Queue<Tuple<SendOrPostCallback, object>>();

            public override void Send(SendOrPostCallback d, object state)
            {
                throw new NotSupportedException("We cannot send to our same thread");
            }

            public override void Post(SendOrPostCallback d, object state)
            {
                lock (items)
                {
                    items.Enqueue(Tuple.Create(d, state));
                }
                workItemsWaiting.Set();
            }

            public void EndMessageLoop()
            {
                Post(_ => done = true, null);
            }

            public void BeginMessageLoop()
            {
                while (!done)
                {
                    Tuple<SendOrPostCallback, object> task = null;
                    lock (items)
                    {
                        if (items.Count > 0)
                        {
                            task = items.Dequeue();
                        }
                    }
                    if (task != null)
                    {
                        task.Item1(task.Item2);
                        if (InnerException != null) // the method threw an exeption
                        {
                            throw new AggregateException("AsyncHelpers.Run method threw an exception.", InnerException);
                        }
                    }
                    else
                    {
                        workItemsWaiting.WaitOne();
                    }
                }
            }

            public override SynchronizationContext CreateCopy()
            => this;
        }
    }

}
