﻿using HtmlAgilityPack;
using ImageMagick;
using System.Net.Http.Headers;
using System.Text;

string runsUrl = "https://gamesdonequick.com/tracker/runs/SGDQ2023";
string donationsUrl = "https://gamesdonequick.com/tracker/donations/SGDQ2023";
string donationXpath = "/html/body/div[1]/h3";
string runsXpath = "/html/body/div[1]/table";
string eventXpath = "/html/body/div[1]/h2";

string eventName = "NO EVENT FOUND";
string donations = "$0000000.00";

string scheduleOne = "NO DATA";
string scheduleTwo = " ";
string scheduleThree = " ";

try
{
    var web = new HtmlWeb();
    var runsPage = web.Load(runsUrl);
    var donationsPage = web.Load(donationsUrl);

    var eventRawText = runsPage.DocumentNode.SelectSingleNode(eventXpath);
    eventName = eventRawText.InnerText.Replace("Run Index\n&mdash;\n", "").Trim();
    
    var donationsRawText = donationsPage.DocumentNode.SelectSingleNode(donationXpath);
    donations = donationsRawText.InnerText.Replace("\n\nTotal (Count):\n", "").Split(" ")[0];

    var runsTable = runsPage.DocumentNode.SelectSingleNode(runsXpath);

    foreach(var row in runsTable.SelectNodes("tr"))
    {
        var cells = row.SelectNodes("td");
        var title = cells[0].InnerText.Replace("\n", "");
        var time = DateTime.Parse(cells[5].InnerText);

        if (time <= DateTime.Now)
        {
            scheduleOne = $"NOW - {title}";
        } else if (scheduleOne.Equals("NO DATA")) // The first row is in the future
        {
            scheduleOne = $"{time.ToString("t")} - {title}";
        } else if (scheduleTwo.Equals(" "))
        {
            scheduleTwo = $"{time.ToString("t")} - {title}";
        } else if (scheduleThree.Equals(" "))
        {
            scheduleThree = $"{time.ToString("t")} - {title}";
        }
    }
} catch (Exception e)
{
    Console.WriteLine("Failed to get data from GDQ tracker.");
}


var donationTextSettings = new MagickReadSettings
{
    TextEncoding = Encoding.Unicode,
    Font = "PICO-8 mono.ttf",
    FontStyle = FontStyleType.Normal,
    FontPointsize = 4,
    FillColor = MagickColors.LimeGreen,
    BackgroundColor = MagickColors.Black,
    TextGravity = Gravity.Center,
    Width = 51,
    Height = 5
};

var eventTextSettings = new MagickReadSettings
{
    TextEncoding = Encoding.Unicode,
    Font = "PICO-8 mono.ttf",
    FontStyle = FontStyleType.Normal,
    FontPointsize = 4,
    FillColor = MagickColors.White,
    BackgroundColor = MagickColors.Black,
    TextGravity = Gravity.West,
    TextInterwordSpacing = 1,
    Width = 59,
    Height = 5
};

var scheduleTextSettings = new MagickReadSettings
{
    TextEncoding = Encoding.Unicode,
    Font = "PICO-8 mono.ttf",
    FontStyle = FontStyleType.Normal,
    FontPointsize = 4,
    FillColor = MagickColors.White,
    BackgroundColor = MagickColors.Black,
    TextGravity = Gravity.West,
    TextInterwordSpacing = 1,
    Width = 59,
    Height = 5
};


if (eventName.Length < 15) // 15 characters visible
{
    eventTextSettings.TextGravity = Gravity.Center;
}


var baseImage = new MagickImage("base.png");
var outputCollection = new MagickImageCollection();

for (int i = 0; i < 60; i++) // Maximum 60 frames
{
    MagickImage image = new MagickImage(baseImage);

    // Make sure the text we want to render fits within our character limit
    var safeEventText = eventName.Length <= 15 ? eventName : eventName.Substring(0, 16);
    var safeScheduleOneText = scheduleOne.Length <= 15 ? scheduleOne : scheduleOne.Substring(0, 16);
    var safeScheduleTwoText = scheduleTwo.Length <= 15 ? scheduleTwo : scheduleTwo.Substring(0, 16);
    var safeScheduleThreeText = scheduleThree.Length <= 15 ? scheduleThree : scheduleThree.Substring(0, 16);

    // Render and composite the text onto the base
    var eventTitleImage = new MagickImage($"caption:{safeEventText}", eventTextSettings);
    var donationImage = new MagickImage($"caption:{donations}", donationTextSettings);
    var scheduleOneImage = new MagickImage($"caption:{safeScheduleOneText}", scheduleTextSettings);
    var scheduleTwoImage = new MagickImage($"caption:{safeScheduleTwoText}", scheduleTextSettings);
    var scheduleThreeImage = new MagickImage($"caption:{safeScheduleThreeText}", scheduleTextSettings);

    image.Composite(eventTitleImage, 2, 22, CompositeOperator.SrcOver);
    image.Composite(donationImage, 8, 30, CompositeOperator.SrcOver);
    image.Composite(scheduleOneImage, 2, 37, CompositeOperator.SrcOver);
    image.Composite(scheduleTwoImage, 2, 45, CompositeOperator.SrcOver);
    image.Composite(scheduleThreeImage, 2, 53, CompositeOperator.SrcOver);
    image.SetBitDepth(8);
    //image.AnimationDelay = 12;
    outputCollection.Add(new MagickImage(image));

    // If the text is longer than our limit, wait for a bit and then scroll it

    if (i > 20)
    {
        if (eventName.Length > 15)
        {
            eventName = eventName.Remove(0, 1);
        }

        if (scheduleOne.Length > 15)
        {
            scheduleOne = scheduleOne.Remove(0, 1);
        }

        if (scheduleTwo.Length > 15)
        {
            scheduleTwo = scheduleTwo.Remove(0, 1);
        }

        if (scheduleThree.Length > 15)
        {
            scheduleThree = scheduleThree.Remove(0, 1);
        }
    } 
}
outputCollection.Write("gdq.gif");

var httpClient = new HttpClient();
var byteArrayContent = new ByteArrayContent(File.ReadAllBytes("gdq.gif"));
httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
byteArrayContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/gif");

var rsp = await httpClient.PostAsync("http://localhost:5000/sendGif", new MultipartFormDataContent
    {
        {byteArrayContent, "\"gif\"", "\"gdq.gif\""},
        {new StringContent("200"), "\"speed\""},
        {new StringContent("false"), "\"skip_first_frame\""}
    });

Environment.Exit(0);