using Google.Apis.Customsearch.v1;
using Google.Apis.Services;
using System;
using System.IO;
using System.Net;
using Newtonsoft.Json;

[Serializable]
public class AppConfig {

    public string apiKey;
    public string searchEngineId;

    public string StoragePath { get; set; } = @$"{Environment.CurrentDirectory}\Temps";
    public string Query { get; set; } = "MBTI 짤방";
    public string FileType = "jpg";
    public int RepeatCount { get; set; } = 1;
    public int Num { get; set; } = 10;
    public int Start { get; set; } = 1;
    public CseResource.ListRequest.SearchTypeEnum SearchType { get; set; } = CseResource.ListRequest.SearchTypeEnum.Image;
}

public enum UserAction {
    None,
    SearchStartByLastQuery,
    SearchStart,
    CheckDuplicateImage,
    InputInfo,
}

internal class Program {
    private static readonly string path = @$"{Environment.CurrentDirectory}";
    private static readonly string ConfigName = @$"{path}\Config.json";

    private static AppConfig Config;
    private static CustomsearchService CustomSearchService;
    private static void Main(string[] args)
    {
        InitInfo();
        while (true) {
            try {
                ShowInfo();

                var action = SelectAction();
                switch (action) {
                    case UserAction.SearchStartByLastQuery:
                        SearchStart(Config.Query);
                        break;
                    case UserAction.SearchStart:
                        SearchStart();
                        break;
                    case UserAction.CheckDuplicateImage:
                        CheckDuplicateImage();
                        break;
                    case UserAction.InputInfo:
                        InputInfo();
                        break;
                }
            } catch (Exception e) {
                Console.WriteLine(e.Message);
                Config.Start = 1;
            }
            Console.WriteLine("--------------------------------------");
        }
    }

    private static void SearchStart(string query = null)
    {
        if (CustomSearchService == null) {
            Console.WriteLine("ApiKey를 입력하세요.");
            return;
        }

        if (Directory.Exists(Config.StoragePath) == false) {
            Directory.CreateDirectory(Config.StoragePath);
        }

        if (string.IsNullOrEmpty(query)) {
            Console.WriteLine($"현재 설정된 저장 경로:{Config.StoragePath}");
            Console.WriteLine("뒤로가기 \"0\" 입력");
            Console.Write("수집할 이미지 검색어를 입력하세요: ");
            query = Console.ReadLine();
            Console.WriteLine();
            if (query == "0") {
                return;
            }
            Config.Query = query;
            Config.Start = 1;
        }

        var listRequest = CustomSearchService.Cse.List();
        listRequest.Q = query;
        listRequest.Cx = Config.searchEngineId;
        listRequest.SearchType = Config.SearchType;
        listRequest.FileType = Config.FileType;
        listRequest.Num = Config.Num;
        listRequest.Start = Config.Start;

        for (int i = 0; i < Config.RepeatCount; i++) {
            listRequest.Start = Config.Start;

            // 검색 요청을 실행하고 결과를 얻습니다.
            var search = listRequest.Execute();
            var items = search.Items;
            foreach (var item in items) {
                var imageUrl = item.Link;
                var extension = Path.GetExtension(imageUrl);
                var questionMarkIndex = extension.IndexOf('?');
                if (questionMarkIndex >= 0) {
                    extension = extension.Substring(0, questionMarkIndex);
                }

                var fileName = $"{Guid.NewGuid()}{extension}";
                var savePath = Path.Combine(Config.StoragePath, fileName);
                try {
                    using (var webClient = new WebClient()) {
                        var imageBytes = webClient.DownloadData(imageUrl);

                        using (var stream = new MemoryStream(imageBytes)) {
                            using (var fileStream = new FileStream(savePath, FileMode.Create)) {
                                stream.CopyTo(fileStream);
                            }
                        }
                    }

                    Console.WriteLine($"Downloaded image: {fileName}");
                } catch (Exception ex) {
                    Console.WriteLine($"Error saving image {fileName}: {ex.Message}");
                }
            }

            Config.Start += Config.Num;
        }

        SaveConfig();

    }

    private static void InitInfo()
    {
        if (Directory.Exists(path) == false) {
            Directory.CreateDirectory(path);
        }

        if (File.Exists(ConfigName)) {
            var jsonString = File.ReadAllText(ConfigName);
            try {
                Config = JsonConvert.DeserializeObject<AppConfig>(jsonString);
            } catch (Exception e) {
                Config = new AppConfig();
                Console.WriteLine(e.Message);
                return;
            }

            CustomSearchService = new CustomsearchService(new BaseClientService.Initializer {
                ApiKey = Config.apiKey
            });
        } else {
            Config = new AppConfig();
            Console.Write("ApiKey 입력:");
            Config.apiKey = Console.ReadLine();
            CustomSearchService = new CustomsearchService(new BaseClientService.Initializer {
                ApiKey = Config.apiKey
            });

            Console.Write("SearchEngineId 입력:");
            Config.searchEngineId = Console.ReadLine();
        }
    }

    private static void ShowInfo()
    {
        Console.WriteLine("------------짤방 수집기 v1------------");
        Console.WriteLine($"ApiKey:{Config.apiKey}");
        Console.WriteLine($"SearchEngineId:{Config.searchEngineId}");
        Console.WriteLine($"검색어: {Config.Query}");
        Console.WriteLine($"검색 반복 횟수:{Config.RepeatCount}");
        Console.WriteLine($"검색 타입:{Config.SearchType}");
        Console.WriteLine($"저장 경로: {Config.StoragePath}");
        Console.WriteLine($"Start:{Config.Start}");
        Console.WriteLine($"Num:{Config.Num}");
        Console.WriteLine("--------------------------------------");
    }

    private static void CheckDuplicateImage()
    {
        if (Directory.Exists(Config.StoragePath) == false) {
            return;
        }

        DuplicateImageChecker.Check(Config);

    }

    private static UserAction SelectAction()
    {
        var result = UserAction.None;
        Console.WriteLine("1.마지막 검색어로 검색");
        Console.WriteLine("2.검색어 입력 검색");
        Console.WriteLine("3.중복 이미지 검사");
        Console.WriteLine("4.사용자 정보 변경");
        Console.Write("숫자를 입력해주세요: ");
        var user_input = Console.ReadLine();
        Console.WriteLine();

        switch (user_input) {
            case "1":
                result = UserAction.SearchStartByLastQuery;
                break;
            case "2":
                result = UserAction.SearchStart;
                break;
            case "3":
                result = UserAction.CheckDuplicateImage;
                break;
            case "4":
                result = UserAction.InputInfo;
                break;
        }
        return result;
    }

    private static void SaveConfig()
    {
        var json = JsonConvert.SerializeObject(Config);
        File.WriteAllText(ConfigName, json);
    }

    private static void InputInfo()
    {
        Console.WriteLine($"1.ApiKey({Config.apiKey}) 변경");
        Console.WriteLine($"2.SearchEngineId({Config.searchEngineId}) 변경");
        Console.WriteLine($"3.검색 반복 횟수({Config.RepeatCount}) 변경");
        Console.WriteLine($"4.받아올 페이지({Config.Num}) 변경");
        Console.WriteLine($"5.시작 숫자({Config.Start}) 변경");
        Console.WriteLine($"6.파일 타입({Config.FileType}) 변경");
        Console.WriteLine($"7.저장 경로 변경({Config.StoragePath}) 변경");

        Console.Write("숫자를 입력해주세요: ");
        var user_input = Console.ReadLine();
        Console.WriteLine();

        switch (user_input) {
            default: return;
            case "1": {
                Console.Write("ApiKey 입력:");
                user_input = Console.ReadLine();
                Console.WriteLine();
                Console.WriteLine($"{Config.apiKey} -> {user_input} 변경했습니다.");
                Config.apiKey = user_input;
                try {
                    CustomSearchService = new CustomsearchService(new BaseClientService.Initializer {
                        ApiKey = Config.apiKey
                    });
                } catch (Exception e) {
                    Console.WriteLine(e.Message);
                    Console.WriteLine("잘 못 입력했습니다.");
                    return;
                }
            }
            break;
            case "2": {
                Console.Write($"현재 SearchEngineId({Config.searchEngineId}) 변경할 SearchEngineId 입력:");
                user_input = Console.ReadLine();
                Console.WriteLine();
                Console.WriteLine($"{Config.searchEngineId} -> {user_input} 변경했습니다.");
                Config.searchEngineId = user_input;
            }
            break;
            case "3": {
                var repeatCount = Config.RepeatCount;
                Console.Write($"현재 검색 반복 횟수({repeatCount}) 변경할 횟수 입력:");
                user_input = Console.ReadLine();
                Console.WriteLine();
                try {
                    Config.RepeatCount = Int32.Parse(user_input);
                    Console.WriteLine($"{repeatCount} -> {user_input} 변경했습니다.");
                } catch (Exception e) {
                    Console.WriteLine(e.Message);
                    Console.WriteLine("숫자를 입력하세요.");
                    return;
                }
            }
            break;
            case "4": {
                var num = Config.Num;
                Console.Write($"현재 검색 횟수({num}) 변경할 횟수 입력:");
                user_input = Console.ReadLine();
                Console.WriteLine();
                try {
                    Config.Num = Int32.Parse(user_input);
                    Console.WriteLine($"{num} -> {user_input} 변경했습니다.");
                } catch (Exception e) {
                    Console.WriteLine(e.Message);
                    Console.WriteLine("숫자를 입력하세요.");
                    return;
                }
            }
            break;
            case "5": {
                var start = Config.Start;
                Console.Write($"현재 시작 숫자({start}) 변경할 입력:");
                user_input = Console.ReadLine();
                Console.WriteLine();
                try {
                    Config.Start = Int32.Parse(user_input);
                    Console.WriteLine($"{start} -> {user_input} 변경했습니다.");
                } catch (Exception e) {
                    Console.WriteLine(e.Message);
                    Console.WriteLine("숫자를 입력하세요.");
                    return;
                }

            }
            break;
            case "6": {
                Console.Write("파일 타입 입력:");
                user_input = Console.ReadLine();
                Console.WriteLine();
                Config.FileType = user_input;
                Console.WriteLine($"{Config.FileType} -> {user_input} 변경했습니다.");
            }
            break;
            case "7": {
                user_input = Console.ReadLine();
                Config.StoragePath = user_input;
                SaveConfig();
            }
            break;
        }

        SaveConfig();
    }
}


