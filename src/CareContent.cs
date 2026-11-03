using System;

namespace XieYiMiao
{
    public sealed class DailyCareCard
    {
        public DailyCareKind Kind { get; private set; }
        public string PawLabel { get; private set; }
        public string Title { get; private set; }
        public string Prompt { get; private set; }
        public string CompletedText { get; private set; }

        public DailyCareCard(
            DailyCareKind kind,
            string pawLabel,
            string title,
            string prompt,
            string completedText)
        {
            Kind = kind;
            PawLabel = pawLabel;
            Title = title;
            Prompt = prompt;
            CompletedText = completedText;
        }
    }

    public sealed class EmotionWeatherCard
    {
        public EmotionWeather Weather { get; private set; }
        public string Symbol { get; private set; }
        public string Title { get; private set; }
        public string Meaning { get; private set; }
        public string Response { get; private set; }

        public EmotionWeatherCard(
            EmotionWeather weather,
            string symbol,
            string title,
            string meaning,
            string response)
        {
            Weather = weather;
            Symbol = symbol;
            Title = title;
            Meaning = meaning;
            Response = response;
        }
    }

    public sealed class PetOutfitOption
    {
        public PetOutfit Outfit { get; private set; }
        public string Name { get; private set; }
        public int RequiredGrowthPoints { get; private set; }
        public string Description { get; private set; }

        public PetOutfitOption(
            PetOutfit outfit,
            string name,
            int requiredGrowthPoints,
            string description)
        {
            Outfit = outfit;
            Name = name;
            RequiredGrowthPoints = requiredGrowthPoints;
            Description = description;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public static class PetOutfitService
    {
        public static readonly PetOutfitOption[] Options =
        {
            new PetOutfitOption(
                PetOutfit.Classic,
                "经典水滴吊坠",
                0,
                "本喵最经典的初始装备"),
            new PetOutfitOption(
                PetOutfit.ApricotScarf,
                "暖杏围巾",
                15,
                "给忙碌的日子围上一点暖意"),
            new PetOutfitOption(
                PetOutfit.SleepCap,
                "小小睡帽",
                35,
                "今晚可以早点收工"),
            new PetOutfitOption(
                PetOutfit.GraduateBag,
                "研究生小书包",
                60,
                "装得下钥匙，装不下无穷无尽的任务"),
            new PetOutfitOption(
                PetOutfit.SafetyGear,
                "护目镜与小扳手",
                100,
                "电气与自动化限定装备")
        };

        public static bool IsUnlocked(PetOutfit outfit, long growthPoints)
        {
            foreach (PetOutfitOption option in Options)
            {
                if (option.Outfit == outfit)
                {
                    return growthPoints >= option.RequiredGrowthPoints;
                }
            }
            return outfit == PetOutfit.Classic;
        }

        public static PetOutfitOption Find(PetOutfit outfit)
        {
            foreach (PetOutfitOption option in Options)
            {
                if (option.Outfit == outfit)
                {
                    return option;
                }
            }
            return Options[0];
        }
    }

    public static class CareContent
    {
        public const string StationName = "猫猫休息站";
        public const string FriendshipPositioning =
            "一只可靠、有点贫嘴的研究生桌面搭子";

        public static readonly DailyCareCard[] DailyCards =
        {
            new DailyCareCard(
                DailyCareKind.Body,
                "身体",
                "身体被照顾过",
                "今天认真吃过东西、喝过水，或者允许身体休息过。",
                "身体收到了一份照顾"),
            new DailyCareCard(
                DailyCareKind.Feelings,
                "感受",
                "感受被承认过",
                "我承认现在的感受，不急着说它不应该存在。",
                "今天的感受被看见了"),
            new DailyCareCard(
                DailyCareKind.Boundary,
                "边界",
                "自己被优先过",
                "今天至少有一次，没有立刻牺牲自己去满足别人。",
                "你为自己留住了一点位置"),
            new DailyCareCard(
                DailyCareKind.Kindness,
                "善意",
                "对自己温柔过",
                "我愿意把安慰朋友时会说的话，也送给自己一次。",
                "这份善意也属于你")
        };

        public static readonly string[] PermissionSlips =
        {
            "今天可以不解释。",
            "不立即回复，也不等于失礼。",
            "做得一般也可以交差。",
            "你不需要先证明价值，才配休息。",
            "今天可以只完成最必要的部分。",
            "别人的失望不自动等于你的错误。",
            "你可以改变主意。",
            "暂时想不明白也没有关系。",
            "今天不用把所有情绪都处理好。",
            "你可以先吃饭，再解决问题。",
            "你可以承认自己不喜欢这件事。",
            "拒绝额外任务，不代表你不负责。",
            "没有及时帮上忙，也不代表你不够好。",
            "今天可以不做那个最懂事的人。",
            "你可以把消息放一会儿再回。",
            "累了不是能力不足，是身体在说话。",
            "你可以请求别人说得更清楚。",
            "不舒服的时候，先离开一下也可以。",
            "今天不复盘，也不会失去成长。",
            "你可以只照顾自己一次。",
            "不是所有关系都需要你主动维持。",
            "你可以让别人承担属于他们的部分。",
            "今天的你不需要被修理。",
            "休息不是奖励，是正常需要。",
            "已经尽力的事情，可以停止追责。"
        };

        public static readonly string[] BoundaryPhrases =
        {
            "我今天接不了这件事。",
            "我需要先确认自己的时间，再回复你。",
            "这部分不在我当前能承担的范围里。",
            "我现在没有余力，可能需要换一个人处理。",
            "这件事需要更明确的要求和截止时间。",
            "我可以帮到这里，后面的部分需要你来完成。",
            "我今天先不做决定，明天再回复。",
            "这个时间对我不合适，我们可以换一个安排。",
            "我理解你很着急，但我现在无法立即处理。",
            "这一次我想优先考虑自己的安排。"
        };

        public static readonly string[] MoodKeys =
        {
            "平静",
            "开心",
            "疲惫",
            "烦躁",
            "委屈",
            "焦虑",
            "说不清"
        };

        public static readonly EmotionWeatherCard[] EmotionWeatherCards =
        {
            new EmotionWeatherCard(
                EmotionWeather.Sunny,
                "晴",
                "晴朗",
                "状态不错",
                "那就让这点晴朗留久一点，不急着拿它去换更多任务。"),
            new EmotionWeatherCard(
                EmotionWeather.Cloudy,
                "云",
                "多云",
                "有点累，但还能过",
                "有点累就算有点累，不用给今天强行加晴天滤镜。"),
            new EmotionWeatherCard(
                EmotionWeather.Drizzle,
                "雨",
                "小雨",
                "心里有点不好受",
                "先别急着说明原因，给自己留五分钟缓冲区。"),
            new EmotionWeatherCard(
                EmotionWeather.Windy,
                "风",
                "大风",
                "烦躁、混乱、被事情推着走",
                "先站稳就好。不是每一阵风都需要你迎面解决。"),
            new EmotionWeatherCard(
                EmotionWeather.Foggy,
                "雾",
                "起雾",
                "不知道自己是什么感受",
                "今天有雾也没关系，不需要现在就把一切想明白。"),
            new EmotionWeatherCard(
                EmotionWeather.Thunderstorm,
                "雷",
                "雷雨",
                "今天真的很难熬",
                "今天确实够呛，先别急着给自己开批斗会。")
        };

        public static readonly string[] QuietBreakResponses =
        {
            "行，脑内会议暂停五分钟。本喵负责在桌边看门。",
            "不用汇报情况。先安静待机一会儿，什么都不推进。",
            "这一会儿没有任务，也没有正确答案，只有缓冲区。",
            "先放空一下。等脑子不那么挤，再决定下一步。"
        };

        public static readonly string[] FunnyResponses =
        {
            "本喵刚刚认真检查过：今天的离谱浓度确实有点超标。",
            "先宣布一件事：这五分钟不参加任何脑内答辩。",
            "你的大脑又想加班开会了，我先去把会议室灯关掉。",
            "如果事情暂时解决不了，可以先给它起个很笨的代号。",
            "本喵建议把今天最烦的事装进纸箱，并在上面写：暂不拆封。",
            "论文可以有未完待续，人也可以。"
        };

        public static readonly string[] FriendSupportResponses =
        {
            "不舒服就是值得被认真对待的感受，不需要先拿出证据。",
            "你已经考虑了很多人的处境，也可以把自己的处境算进去。",
            "别人的着急，不会自动变成你的全部责任。",
            "你可以善良，也可以有边界；这两件事并不冲突。",
            "累不是能力不足的证明，只是你确实消耗了很多。",
            "即使这次没有处理得完美，你也不需要因此全盘否定自己。",
            "你不需要永远做那个最方便、最懂事的人。",
            "今天的你不是一个等待修正的问题。"
        };

        public static readonly string[] PaperDismissalResponses =
        {
            "纸团已被本喵拍进看不见的角落，今天不再开庭。",
            "丢掉了。它要是回来，我就假装没开门。",
            "已处理：不分析、不归档、不追加三千字复盘。",
            "这张纸条今天毕业了，不需要答辩。",
            "本喵已经把它压扁了。剩下的力气先还给你。"
        };

        public static readonly string[] GentleBoundaryPhrases =
        {
            "我想先确认一下自己的安排，再回复你，可以吗？",
            "我今天的精力比较有限，这件事可能没办法马上接下。",
            "我可以帮到这里，后面的部分需要你再安排一下。",
            "这个时间对我不太合适，我们可以换一个时间吗？"
        };

        public static readonly string[] NeutralBoundaryPhrases =
        {
            "我需要先确认自己的时间，再回复你。",
            "这部分不在我当前能承担的范围里。",
            "我现在无法立即处理，请重新确认负责人和截止时间。",
            "我可以完成这一部分，其余部分需要另外安排。"
        };

        public static readonly string[] FirmBoundaryPhrases =
        {
            "我今天接不了这件事。",
            "这项任务不属于我当前负责的范围。",
            "我现在没有余力，请不要把它临时转交给我。",
            "这次我会优先按照自己已经确定的安排进行。"
        };

        public static readonly string[] ClosingPraise =
        {
            "今天已经做了不少事。辛苦不是只有成功以后才算数。",
            "你把今天走到了这里，这件事本身就值得被承认。",
            "有些努力没有立刻变成结果，但它们确实发生过。",
            "今天的你已经承担了很多，可以停止证明自己了。"
        };

        public static readonly string[] ClosingFunny =
        {
            "今日脑内办公室现已断电，临时加班申请一律退回。",
            "今天到这里。剩下的问题请明天携带有效证件再来。",
            "本喵已把未完成事项锁进抽屉，今晚它们没有探视权。",
            "今日研究生营业时间结束，请带好手机和钥匙回宿舍。"
        };

        public static readonly string[] ClosingQuiet =
        {
            "今天到这里，也可以算完整的一天。",
            "没做完的事情留在桌上，不需要一起带回去。",
            "你可以先回去，问题明天还会在原地。",
            "现在不用总结出意义，先让今天结束。"
        };

        public static readonly string[] MorningWhispers =
        {
            "早。今天不用一开始就跑得很快。",
            "先坐稳，再决定今天从哪里开始。",
            "如果还没完全醒，本喵批准缓慢加载。"
        };

        public static readonly string[] DayWhispers =
        {
            "本喵待机中，不检查进度。",
            "做得一般，也不妨碍这一步已经在发生。",
            "记得把自己的安排，也算进安排里。",
            "如果脑子太挤，可以先只留下一件事。"
        };

        public static readonly string[] NoonWhispers =
        {
            "到饭点附近了，先别拿忙当午饭。",
            "热饭比临时多做一件事更值得排进日程。",
            "本喵没有催你，只是想起了饭。"
        };

        public static readonly string[] EveningWhispers =
        {
            "天已经晚了，没做完的不等于做错了。",
            "今天可以开始收尾，不必等到所有问题同意。",
            "回去的时候，别忘了带钥匙，别带走全部工作。"
        };

        public static readonly string[] LateNightWhispers =
        {
            "今天已经够长了。",
            "问题可以留在桌上，你不用和它一起值夜班。",
            "本喵的小书包已经准备好了，随时可以回去。",
            "深夜不适合审判自己，容易误判。"
        };

        public static string RandomPermission(Random random)
        {
            Random chooser = random ?? new Random();
            return PermissionSlips[chooser.Next(PermissionSlips.Length)];
        }

        public static EmotionWeatherCard FindWeather(
            EmotionWeather weather)
        {
            foreach (EmotionWeatherCard card in EmotionWeatherCards)
            {
                if (card.Weather == weather)
                {
                    return card;
                }
            }
            return null;
        }

        public static string RandomFrom(string[] values, Random random)
        {
            if (values == null || values.Length == 0)
            {
                return "";
            }
            Random chooser = random ?? new Random();
            return values[chooser.Next(values.Length)];
        }

        public static string[] BoundarySet(string tone)
        {
            if (tone == "gentle")
            {
                return GentleBoundaryPhrases;
            }
            if (tone == "firm")
            {
                return FirmBoundaryPhrases;
            }
            return NeutralBoundaryPhrases;
        }

        public static string ClosingFor(string style, Random random)
        {
            if (style == "praise")
            {
                return RandomFrom(ClosingPraise, random);
            }
            if (style == "funny")
            {
                return RandomFrom(ClosingFunny, random);
            }
            return RandomFrom(ClosingQuiet, random);
        }

        public static string[] WhispersForHour(int hour)
        {
            if (hour >= 23 || hour < 5)
            {
                return LateNightWhispers;
            }
            if (hour < 10)
            {
                return MorningWhispers;
            }
            if (hour >= 11 && hour <= 13)
            {
                return NoonWhispers;
            }
            if (hour >= 18)
            {
                return EveningWhispers;
            }
            return DayWhispers;
        }
    }
}
