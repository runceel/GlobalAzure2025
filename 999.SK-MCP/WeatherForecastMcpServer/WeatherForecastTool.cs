using System.ComponentModel;
using ModelContextProtocol.Server;

namespace WeatherForecastMcpServer;

[McpServerToolType]
public class WeatherForecastTool
{
    [McpServerTool]
    [Description("今の日時を取得します。")]
    public DateTimeOffset GetLocalNow() => 
        TimeProvider.System.GetLocalNow();

    [McpServerTool]
    [Description("指定した場所の天気予報を取得します。")]
    public string GetWeatherForecast(
        [Description("天気を取得する日付")]
        DateTimeOffset date,
        [Description("場所の名前")]
        string location) => 
        location switch
        {
            "東京" => $"{date} の東京の天気は「晴れ」です。",
            "大阪" => $"{date} の大阪の天気は「曇り」です。",
            "名古屋" => $"{date} の名古屋の天気は「雨」です。",
            _ => $"{date} の{location} の天気は「空から蛙」です。",
        };
}
