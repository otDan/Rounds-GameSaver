using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using BepInEx;
using GameSaver.Menu;
using GameSaver.Util.Web.Response;
using Newtonsoft.Json;
using TMPro;
using UnboundLib;
using UnityEngine;

namespace GameSaver.Util.Web
{
    internal class WebManager
    {
        private const string PasteToken = "un2T1H6PaGg5zZOfPcoCvlUa9WN6X5itKk8NQLlkG";

        public static IEnumerator SendSaves(TMP_InputField inputField, GameObject spinner)
        {
            GameSaver.Instance.Log("Started GEN");
            var guid = Guid.NewGuid();
            string cacheDirectoryPath = Paths.CachePath + "/GameSaver";
            if (!Directory.Exists(cacheDirectoryPath))
                Directory.CreateDirectory(cacheDirectoryPath);
            string cacheFilePath = cacheDirectoryPath + "/" + guid;
            ZipFile.CreateFromDirectory(SaveManager.SavesPath, cacheFilePath);
            byte[] bytes = File.ReadAllBytes(cacheFilePath);
            string saves = Convert.ToBase64String(bytes);
            GameSaver.Instance.Log("Started POST");
            var sendSavesTask = SendSavesAsync(guid.ToString(), saves);
            yield return new WaitUntil(() => sendSavesTask.IsCompleted);
            string result = sendSavesTask.Result;

            PostPaste.PostPasteResponse json = JsonConvert.DeserializeObject<PostPaste.PostPasteResponse>(result);
            // GameSaver.Instance.Log("POST Answer: " + result);
            inputField.text = json != null ? json.id : "ERROR";
            spinner.SetActive(false);
            yield return null;
        }

        public static IEnumerator GetSaves(Canvas uiExportImportCanvas, TMP_InputField inputField, string code, GameObject spinner, TextMeshProUGUI placeholder, Color initialColor, Color errorColor)
        {
            GameSaver.Instance.Log("Started GET");
            var getSavesTask = GetSavesFromId(code);
            yield return new WaitUntil(() => getSavesTask.IsCompleted);
            string result = getSavesTask.Result;

            try
            {
                GetPaste.GetPasteResponse json = JsonConvert.DeserializeObject<GetPaste.GetPasteResponse>(result);
                if (json == null)
                {
                    spinner.SetActive(false);
                    placeholder.color = errorColor;
                    placeholder.text = "JSON ERROR";
                    SaveLoadMenu.instance.ExecuteAfterSeconds(1, () =>
                    {
                        placeholder.color = initialColor;
                        placeholder.text = "CODE HERE...";
                        inputField.interactable = true;
                    });
                    yield break;
                }

                foreach (var pasteSection in json.paste.sections)
                {
                    byte[] data = Convert.FromBase64String(pasteSection.contents);
                    string cacheDirectoryPath = Paths.CachePath + "/GameSaver";
                    if (!Directory.Exists(cacheDirectoryPath))
                        Directory.CreateDirectory(cacheDirectoryPath);
                    string cacheUnZipDirectoryPath = cacheDirectoryPath + "/UnZip";
                    if (!Directory.Exists(cacheUnZipDirectoryPath))
                        Directory.CreateDirectory(cacheUnZipDirectoryPath);
                    string cacheUniqueDirectoryPath = cacheUnZipDirectoryPath + "/" + Guid.NewGuid();
                    if (!Directory.Exists(cacheUniqueDirectoryPath))
                        Directory.CreateDirectory(cacheUniqueDirectoryPath);

                    string cacheFilePath = cacheDirectoryPath + "/" + Guid.NewGuid();
                    using var stream = File.Create(cacheFilePath);
                    stream.Write(data, 0, data.Length);
                    stream.Dispose();

                    ZipFile.ExtractToDirectory(cacheFilePath, cacheUniqueDirectoryPath);
                    CloneDirectory(cacheUniqueDirectoryPath, SaveManager.SavesPath);
                }

                spinner.SetActive(false);
                inputField.interactable = true;
                uiExportImportCanvas.enabled = false;
            }
            catch(JsonException)
            {
                spinner.SetActive(false);
                placeholder.color = errorColor;
                placeholder.text = "IMPORT ERROR";
                SaveLoadMenu.instance.ExecuteAfterSeconds(1, () =>
                {
                    placeholder.color = initialColor;
                    placeholder.text = "CODE HERE...";
                    inputField.interactable = true;
                });
            }
            // GameSaver.Instance.Log("GET Answer: " + json?.paste.sections[0].contents);
            // inputField.text = json?.paste.sections[0].contents;
            
            yield return null;
        }

        private static void CloneDirectory(string root, string dest)
        {
            foreach (var directory in Directory.GetDirectories(root))
            {
                var newDirectory = Path.Combine(dest, Path.GetFileName(directory));
                //Create the directory if it doesn't already exist
                if (Directory.Exists(newDirectory))
                    continue;
                Directory.CreateDirectory(newDirectory);
                //Recursively clone the directory
                CloneDirectory(directory, newDirectory);
            }

            foreach (var file in Directory.GetFiles(root))
            {
                File.Copy(file, Path.Combine(dest, Path.GetFileName(file)));
            }
        }

        // curl "https://api.paste.ee/v1/pastes"
        //     -X "POST"
        //     -H "Content-Type: application/json"
        //     -H "X-Auth-Token: meowmeowmeow"
        //     -D '{"description":"test","sections":[{"name":"Section1","syntax":"autodetect","contents":"Testing!"}]}'
        private static async Task<string> SendSavesAsync(string guid, string data)
        {
            using var httpClient = new HttpClient();
            using var request = new HttpRequestMessage(new HttpMethod("POST"), "https://api.paste.ee/v1/pastes");
            request.Headers.TryAddWithoutValidation("X-Auth-Token", PasteToken);
            request.Content = new StringContent("{\"description\":\"" + guid + "\",\"sections\":[{\"name\":\"save\",\"syntax\":\"autodetect\",\"contents\":\"" + data + "\"}]}");
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json"); 

            var response = await httpClient.SendAsync(request);
            var contents = await response.Content.ReadAsStringAsync();
            // GameSaver.Instance.Log(contents);
            return contents;
        }

        // curl "https://api.paste.ee/v1/pastes/<id>"
        //     -H "X-Auth-Token: meowmeowmeow"
        private static async Task<string> GetSavesFromId(string id)
        {
            using var httpClient = new HttpClient();
            using var request = new HttpRequestMessage(new HttpMethod("GET"), "https://api.paste.ee/v1/pastes/" + id);
            request.Headers.TryAddWithoutValidation("X-Auth-Token", PasteToken); 

            var response = await httpClient.SendAsync(request);
            var contents = await response.Content.ReadAsStringAsync();
            // GameSaver.Instance.Log(contents);
            return contents;
        }
    }
}
