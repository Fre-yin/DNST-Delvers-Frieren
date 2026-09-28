using HarmonyLib;
#if BEPINEX
using global::Refactor.Setting;
#else
using Il2CppRefactor.Setting;
#endif
#if BEPINEX
using global::Refactor.Util;
#else
using Il2CppRefactor.Util;
#endif
#if BEPINEX
using global::Util.Sheet;
#else
using Il2CppUtil.Sheet;
#endif

namespace FrierenPortrait;

internal static class FrierenLocalization
{
    internal const string BiographyKey = "TEXTKEY_UNITBIO_DannyFrieren";
    internal const string BiographyNameFormat = "<color=#E4DFDB>{0}</color>";
    internal const string TraitNameKey = "TEXTKEY_AFFECTER_DannyElfArchmage_NAME";
    internal const string TraitMasteryKey = "TEXTKEY_AFFECTER_DannyElfArchmage_MASTERY";
    internal const string TraitManaReserveKey = "TEXTKEY_AFFECTER_DannyElfArchmage_MANA_RESERVE";
    internal const string TraitPracticedMageKey = "TEXTKEY_AFFECTER_DannyElfArchmage_PRACTICED_MAGE";
    internal const string TraitDescriptionKey = "TEXTKEY_AFFECTER_DannyElfArchmage_DESC";
    internal const string TraitFlavorKey = "TEXTKEY_AFFECTER_DannyElfArchmage_FLAVOR";
    internal const string BookloverNameKey = "TEXTKEY_AFFECTER_DannyBooklover_NAME";
    internal const string BookloverDescKey = "TEXTKEY_AFFECTER_DannyBooklover_DESC";
    internal const string BookloverFlavorKey = "TEXTKEY_AFFECTER_DannyBooklover_FLAVOR";
    internal const string BookloverMoodNameKey = "TEXTKEY_AFFECTER_DannyBookloverMood_NAME";
    internal const string BookloverMoodDescKey = "TEXTKEY_AFFECTER_DannyBookloverMood_DESC";
    internal const string BookloverMoodFlavorKey = "TEXTKEY_AFFECTER_DannyBookloverMood_FLAVOR";

    private static readonly TextKeyTableData[] Rows =
    {
        Row(TraitNameKey,
            "Elven Archmage", "엘프 대마법사", "Archimage elfe", "Elfische Erzmagierin",
            "Эльфийская архимагиня", "精灵大魔法使", "精靈大魔法使", "エルフの大魔法使い",
            "Archimaga elfa", "Arquimaga élfica"),
        Row(BiographyKey,
            Biography("{0} spent centuries wandering through kingdoms, villages, and ruins, collecting spells others had long forgotten. When the dungeon core awakened once more and new portals rendered the land uninhabitable, she joined the expedition to investigate the failed experiments of the fallen Empyreum. Fame and fortune held little appeal for her, but rare grimoires and little spells might lie hidden among the ruins."),
            Biography("{0}은 수백 년 동안 왕국과 마을, 폐허를 떠돌며 다른 이들이 오래전에 잊은 마법을 모았다. 던전 코어가 다시 깨어나고 새로 열린 포털들로 인해 그 땅이 더는 사람이 살 수 없는 곳으로 변하자, 멸망한 엠피리움의 실패한 실험을 조사하기 위해 원정대에 합류했다. 명성과 부에는 거의 관심이 없었지만, 폐허 사이에는 희귀한 마도서와 소소한 마법들이 숨겨져 있을지도 모른다."),
            Biography("{0} a parcouru pendant des siècles des royaumes, des villages et des ruines, collectionnant des sorts que d’autres avaient oubliés depuis longtemps. Lorsque le cœur du donjon s’est de nouveau éveillé et que de nouveaux portails ont rendu le pays inhabitable, elle a rejoint l’expédition pour enquêter sur les expériences ratées de l’Empyreum disparu. La gloire et la richesse l’attiraient peu, mais les ruines pourraient receler de rares grimoires et de petits sorts."),
            Biography("{0} durchwanderte über Jahrhunderte Königreiche, Dörfer und Ruinen und sammelte Zauber, die andere längst vergessen hatten. Als der Dungeonkern erneut erwachte und neue Portale das Land unbewohnbar machten, schloss sie sich der Expedition an, um die missglückten Experimente des untergegangenen Empyreums zu untersuchen. Ruhm und Reichtum reizten sie kaum, doch zwischen den Ruinen könnten seltene Grimoires und kleine Zauber verborgen liegen."),
            Biography("{0} веками странствовала по королевствам, деревням и руинам, собирая заклинания, о которых другие давно забыли. Когда ядро подземелья вновь пробудилось, а новые порталы сделали этот край непригодным для жизни, она присоединилась к экспедиции, чтобы исследовать неудачные эксперименты павшего Эмпирея. Слава и богатство мало её привлекали, но среди руин могли скрываться редкие гримуары и небольшие заклинания."),
            Biography("数百年来，{0}游历王国、村庄与遗迹，收集那些早已被他人遗忘的魔法。当地下城核心再次苏醒，新出现的传送门使这片土地变得无法居住时，她加入了远征队，以调查覆灭的Empyreum留下的失败实验。名声和财富对她几乎毫无吸引力，但遗迹之中或许藏着珍稀的魔导书和一些小魔法。"),
            Biography("數百年來，{0}遊歷王國、村莊與遺跡，蒐集那些早已被他人遺忘的魔法。當地下城核心再次甦醒，新出現的傳送門使這片土地變得無法居住時，她加入了遠征隊，以調查覆滅的Empyreum留下的失敗實驗。名聲和財富對她幾乎毫無吸引力，但遺跡之中或許藏著珍稀的魔導書和一些小魔法。"),
            Biography("{0}は何世紀にもわたって王国や村、遺跡を渡り歩き、ほかの者たちがとうに忘れた魔法を集めてきた。ダンジョンコアが再び目覚め、新たに開いたポータルが大地を人の住めない場所に変えると、滅びたエンピレウムの失敗に終わった実験を調査するため、遠征隊に加わった。名声や富にはほとんど興味がなかったが、遺跡の中には珍しい魔導書やささやかな魔法が隠されているかもしれない。"),
            Biography("{0} pasó siglos recorriendo reinos, aldeas y ruinas, reuniendo hechizos que otros habían olvidado hacía mucho tiempo. Cuando el núcleo de la mazmorra volvió a despertar y nuevos portales hicieron inhabitable el territorio, se unió a la expedición para investigar los experimentos fallidos del desaparecido Empyreum. La fama y la fortuna apenas la atraían, pero entre las ruinas podrían ocultarse grimorios raros y pequeños hechizos."),
            Biography("{0} passou séculos percorrendo reinos, vilarejos e ruínas, reunindo feitiços que outros já tinham esquecido muito tempo antes. Quando o núcleo da masmorra voltou a despertar e novos portais tornaram o território inabitável, ela se juntou à expedição para investigar os experimentos fracassados do extinto Empyreum. A fama e a fortuna pouco lhe interessavam, mas grimórios raros e pequenos feitiços poderiam estar escondidos entre as ruínas.")),
        Row(TraitMasteryKey,
            "Master of All Schools of Magic", "모든 마법 학파의 대가", "Maîtrise de toutes les écoles de magie",
            "Meister aller Magieschulen", "Мастер всех школ магии", "精通所有魔法学派", "精通所有魔法學派",
            "あらゆる魔法系統の達人", "Maestra de todas las escuelas de magia", "Mestra de todas as escolas de magia"),
        Row(TraitManaReserveKey,
            "Condensed Mana Pool", "응축된 마나", "Réserve de mana condensée", "Verdichtete Manareserve",
            "Концентрированный запас маны", "凝聚的魔力储备", "凝聚的魔力儲備", "凝縮された魔力",
            "Reserva de maná condensada", "Reserva de mana condensada"),
        Row(TraitPracticedMageKey,
            "Practiced Mage", "숙련된 마법사", "Magicienne aguerrie", "Geübte Magierin",
            "Опытная волшебница", "熟练的魔法使", "熟練的魔法使", "熟練の魔法使い",
            "Maga experimentada", "Maga experiente"),
        Row(TraitDescriptionKey,
            "For more than a millennium, this mage has explored the secrets of magic. Her knowledge allows her to cast spells from every school of magic.",
            "천 년이 넘는 세월 동안 이 마법사는 마법의 비밀을 탐구해 왔습니다. 그녀의 지식은 모든 마법 학파의 주문을 사용할 수 있게 해 줍니다.",
            "Depuis plus d’un millénaire, cette magicienne explore les secrets de la magie. Son savoir lui permet de lancer des sorts de toutes les écoles de magie.",
            "Seit mehr als einem Millennium ergründet diese Magierin die Geheimnisse der Magie. Ihr Wissen ermöglicht es ihr, Zauber sämtlicher Magieschulen zu wirken.",
            "Более тысячи лет эта волшебница постигает тайны магии. Её знания позволяют ей применять заклинания всех школ магии.",
            "一千多年来，这位魔法使一直在探究魔法的奥秘。她的知识使她能够施展所有魔法学派的法术。",
            "一千多年來，這位魔法使一直在探究魔法的奧祕。她的知識使她能夠施展所有魔法學派的法術。",
            "千年以上にわたり、この魔法使いは魔法の神秘を探究してきた。その知識により、あらゆる魔法系統の呪文を扱うことができる。",
            "Durante más de un milenio, esta maga ha estudiado los secretos de la magia. Su conocimiento le permite lanzar hechizos de todas las escuelas de magia.",
            "Há mais de um milênio, esta maga investiga os segredos da magia. Seu conhecimento permite que ela lance feitiços de todas as escolas de magia."),
        Row(TraitFlavorKey,
            "What kind of magic do I like most? The kind that makes a simple field of flowers bloom.",
            "내가 가장 좋아하는 마법? 그저 평범한 꽃밭을 피워 내는 마법.",
            "Quelle magie est-ce que je préfère ? Celle qui fait fleurir un simple champ de fleurs.",
            "Welche Magie ich am meisten mag? Die, die ein einfaches Blumenfeld erblühen lässt.",
            "Какая магия мне нравится больше всего? Та, от которой расцветает самое обычное поле цветов.",
            "我最喜欢什么魔法？能让一片普通花田盛开的魔法。", "我最喜歡什麼魔法？能讓一片普通花田盛開的魔法。",
            "一番好きな魔法？ 何でもない野原に花を咲かせる魔法かな。",
            "¿Qué magia me gusta más? La que hace florecer un sencillo campo de flores.",
            "De qual magia eu mais gosto? Daquela que faz as flores de um campo simples desabrocharem."),
        Row(BookloverNameKey,
            "Booklover", "책벌레", "Bibliophile", "Bücherwurm", "Книголюб", "爱书人", "愛書人", "本好き",
            "Amante de los libros", "Amante dos livros"),
        Row(BookloverDescKey,
            "When this character uses a technique book from their inventory, the book is consumed and they gain 2 main skill points, 2 secondary skill points, and +4 mood for 20 minutes.",
            "이 캐릭터의 소지품에서 기술서 1권을 사용하면 해당 기술서를 소모하고, 주 스킬 포인트와 보조 스킬 포인트를 각각 2점씩 얻으며 20분 동안 기분 +4 효과를 받습니다.",
            "Lorsqu’un livre de techniques de l’inventaire de ce personnage est utilisé, il est consommé et ce personnage gagne 2 points de compétence principale, 2 points de compétence secondaire et +4 de moral pendant 20 minutes.",
            "Benutzt diese Figur ein Technikbuch aus ihrem Inventar, wird das Buch verbraucht und sie erhält 2 Hauptskillpunkte, 2 Sekundärskillpunkte und +4 Laune für 20 Minuten.",
            "Используйте книгу техник из инвентаря этого персонажа: книга будет израсходована, а персонаж получит 2 очка основных навыков, 2 очка второстепенных навыков и +4 к настроению на 20 минут.",
            "从该角色的物品栏中使用技巧书时，会消耗1本技巧书，获得2点主技能点和2点副技能点，并使心情提高4点，持续20分钟。",
            "從該角色的物品欄中使用技巧書時，會消耗1本技巧書，獲得2點主技能點和2點副技能點，並使心情提高4點，持續20分鐘。",
            "このキャラクターの所持品にある技術書を1冊使うと、その本を消費してメインとサブのスキルポイントを2ずつ獲得し、気分が20分間4上昇する。",
            "Cuando este personaje usa un libro de técnicas de su inventario, el libro se consume y el personaje obtiene 2 puntos de habilidad principal, 2 puntos de habilidad secundaria y +4 de ánimo durante 20 minutos.",
            "Quando este personagem usa um livro de técnicas do próprio inventário, o livro é consumido e o personagem recebe 2 pontos de habilidade principal, 2 pontos de habilidade secundária e +4 de humor por 20 minutos."),
        Row(BookloverFlavorKey,
            "Just one more chapter. Sleep is overrated anyway.",
            "한 장만 더. 잠은 원래 과대평가된 거야.",
            "Encore un chapitre. Le sommeil est surestimé, de toute façon.",
            "Nur noch ein Kapitel. Schlaf wird ohnehin überbewertet.",
            "Ещё одну главу. Сон и так переоценён.",
            "再读一章就好。反正睡觉也没那么重要。",
            "再讀一章就好。反正睡覺也沒那麼重要。",
            "あと一章だけ。どうせ睡眠なんて大したものじゃない。",
            "Solo un capítulo más. Dormir está sobrevalorado de todos modos.",
            "Só mais um capítulo. Dormir é superestimado mesmo."),
        Row(BookloverMoodNameKey,
            "Joy of Reading", "독서의 즐거움", "Joie de lire", "Lesefreude", "Радость чтения", "阅读之乐", "閱讀之樂", "読書の喜び",
            "Placer de leer", "Prazer de ler"),
        Row(BookloverMoodDescKey,
            "Reading a textbook satisfies the thirst for knowledge and leaves a feeling of contentment for 20 minutes.",
            "교재를 읽으면 지식에 대한 갈증이 해소되고 20분 동안 만족감을 느낍니다.",
            "Lire un manuel étanche la soif de savoir et procure un sentiment de satisfaction pendant 20 minutes.",
            "Das Lesen eines Lehrbuchs stillt den Wissensdurst und sorgt 20 Minuten lang für Zufriedenheit.",
            "Чтение учебника утоляет жажду знаний и дарит чувство удовлетворения на 20 минут.",
            "阅读一本教材能满足求知欲，并带来持续20分钟的满足感。",
            "閱讀一本教材能滿足求知慾，並帶來持續20分鐘的滿足感。",
            "教本を読むと知識欲が満たされ、満足感が20分間続く。",
            "Leer un libro de texto sacia la sed de conocimiento y deja una sensación de satisfacción durante 20 minutos.",
            "Ler um livro didático sacia a sede de conhecimento e deixa uma sensação de satisfação por 20 minutos."),
        Row(BookloverMoodFlavorKey,
            "A good book makes everything brighter.", "좋은 책은 모든 것을 밝게 만듭니다.",
            "Un bon livre illumine tout.", "Ein gutes Buch hellt alles auf.",
            "Хорошая книга делает всё светлее.", "好书让一切都更加明亮。", "好書讓一切都更加明亮。",
            "いい本は、すべてを明るくしてくれる。", "Un buen libro lo ilumina todo.",
            "Um bom livro deixa tudo mais alegre."),
    };

    private static IntPtr registeredSheet;
    private static LanguageType registeredLanguage;

    internal static void ResetLifecycle()
    {
        registeredSheet = IntPtr.Zero;
        registeredLanguage = default;
    }

    internal static string TraitName => Get(TraitNameKey, "Elfische Erzmagierin");
    internal static string TraitMastery => Get(TraitMasteryKey, "Meister aller Magieschulen");
    internal static string TraitManaReserve => Get(TraitManaReserveKey, "Verdichtete Manareserve");
    internal static string TraitPracticedMage => Get(TraitPracticedMageKey, "Geübte Magierin");
    internal static string TraitDescription => Get(TraitDescriptionKey,
        "Seit mehr als einem Millennium ergründet diese Magierin die Geheimnisse der Magie. Ihr Wissen ermöglicht es ihr, Zauber sämtlicher Magieschulen zu wirken.");
    internal static string TraitFlavor => Get(TraitFlavorKey,
        "Welche Magie ich am meisten mag? Die, die ein einfaches Blumenfeld erblühen lässt.");

    internal static void EnsureCurrent()
    {
        var manager = DataSheetManager.Instance;
        var sheet = manager?._text;
        if (sheet?._tableData == null || sheet._textTable == null) return;
        var language = LanguageSetting.GetCurrentLanguage();
        if (registeredSheet == sheet.Pointer && registeredLanguage == language
            && sheet._textTable.TryGetValue(TraitNameKey, out var current)
            && current == TextKeyLanguageTool.GetPreferredText(
                TextKeyLanguageTool.GetTextByLanguage(Rows[0], language), Rows[0].English)) return;
        Register(sheet, language);
    }

    internal static void Register(TextSheet sheet, LanguageType language)
    {
        if (sheet?._tableData == null || sheet._textTable == null) return;
        foreach (var row in Rows)
        {
            var activeRow = row;
            if (sheet._tableData.TryGetValue(row.Key, out var existing))
            {
                if (existing.Pointer != row.Pointer && !SameTranslations(existing, row))
                    throw new InvalidOperationException("Frieren-TextKey ist bereits belegt: " + row.Key);
                activeRow = existing;
            }
            else sheet._tableData.Add(row.Key, row);

            var localized = TextKeyLanguageTool.GetTextByLanguage(activeRow, language);
            sheet._textTable[row.Key] = TextKeyLanguageTool.GetPreferredText(localized, activeRow.English);
        }
        registeredSheet = sheet.Pointer;
        registeredLanguage = language;
    }

    internal static string Get(string key, string fallback)
    {
        EnsureCurrent();
        var sheet = DataSheetManager.Instance?._text;
        if (sheet?._textTable != null && sheet._textTable.TryGetValue(key, out var value)
            && !string.IsNullOrWhiteSpace(value)) return value;
        return fallback;
    }

    internal static string Format(string key, string fallback, params object[] values)
        => string.Format(Get(key, fallback), values);

    internal static void ValidateAllLanguages()
    {
        foreach (var row in Rows)
        {
            foreach (var language in Enum.GetValues<LanguageType>())
            {
                var text = TextKeyLanguageTool.GetTextByLanguage(row, language);
                if (string.IsNullOrWhiteSpace(text))
                    throw new InvalidOperationException($"Leere Frieren-Übersetzung: {row.Key}/{language}");
                if (text.IndexOf('\u2013') >= 0 || text.IndexOf('\u2014') >= 0)
                    throw new InvalidOperationException($"Frieren-Übersetzung enthält einen Langstrich: {row.Key}/{language}");
                if (row.Key == BiographyKey && CountOccurrences(text, "{0}") != 1)
                    throw new InvalidOperationException($"Frieren-Platzhalter ungültig: {row.Key}/{language}");
                if (row.Key == BiographyKey && CountOccurrences(text, BiographyNameFormat) != 1)
                    throw new InvalidOperationException($"Frieren-Biografiename ist nicht hervorgehoben: {language}");
            }
        }
    }

    internal static void ValidateLanguageSwitch(TextSheet sheet)
    {
        if (sheet == null) throw new InvalidOperationException("Texttabelle fehlt beim Sprachtest.");
        var original = LanguageSetting.GetCurrentLanguage();
        try
        {
            foreach (var language in Enum.GetValues<LanguageType>())
            {
                // Exercise the real native parse path. Its Harmony postfix must
                // republish every custom row after the active dictionary clears.
                sheet.ParseMatchingLanguage(language);
                foreach (var row in Rows)
                {
                    var expected = TextKeyLanguageTool.GetPreferredText(
                        TextKeyLanguageTool.GetTextByLanguage(row, language), row.English);
                    if (!sheet._textTable.TryGetValue(row.Key, out var actual) || actual != expected)
                        throw new InvalidOperationException($"Frieren-Sprachwechsel fehlgeschlagen: {row.Key}/{language}");
                }
            }
        }
        finally { sheet.ParseMatchingLanguage(original); }
    }

    private static bool SameTranslations(TextKeyTableData left, TextKeyTableData right)
    {
        foreach (var language in Enum.GetValues<LanguageType>())
            if (TextKeyLanguageTool.GetTextByLanguage(left, language)
                != TextKeyLanguageTool.GetTextByLanguage(right, language)) return false;
        return true;
    }

    private static int CountOccurrences(string text, string value)
        => (text.Length - text.Replace(value, string.Empty).Length) / value.Length;

    private static string Biography(string text)
        => text.Replace("{0}", BiographyNameFormat);

    private static TextKeyTableData Row(string key, string en, string ko, string fr, string de, string ru,
        string zhHans, string zhHant, string ja, string es, string ptBr) => new()
    {
        Key = key,
        English = en,
        Korean = ko,
        French = fr,
        German = de,
        Russian = ru,
        ChineseSimplified = zhHans,
        ChineseTraditional = zhHant,
        Japanese = ja,
        Spanish = es,
        PortugueseBrazil = ptBr
    };
}

[HarmonyPatch(typeof(TextSheet), nameof(TextSheet.ParseMatchingLanguage))]
internal static class FrierenLanguageRegistrationPatch
{
    private static void Postfix(TextSheet __instance, LanguageType __0)
        => FrierenLocalization.Register(__instance, __0);
}
