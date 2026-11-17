using System.Collections.Generic;

namespace SHHH.Game
{
    /// <summary>
    /// Mom's English script - GENERATED from Tools/audio_v8/mom_lines_v8.json (V8, 2026-11-09, original lines for the continuous match).
    /// Audio "vo_&lt;id&gt;" (offline voice bank, Tools/audio_v8/make_vo.py) plays when present; subtitles always shown. Lines with {name}:
    /// the "vo_name_&lt;name&gt;" clip is spoken first, then the line.
    /// </summary>
    public static class MomLines
    {
        static readonly Dictionary<string, string> L = new Dictionary<string, string>
        {
            ["mom_stir_1"] = "Mmm... five more minutes...",
            ["mom_stir_2"] = "Hnn... is somebody... rattling?",
            ["mom_stir_3"] = "Mm. Whoever is in the kitchen... hush.",
            ["mom_stir_4"] = "Sleepy Mom... is still... sleeping...",
            ["mom_suspicious_1"] = "Hm? What was that clink?",
            ["mom_suspicious_2"] = "I know that sound. That's a plate.",
            ["mom_suspicious_3"] = "Somebody is very busy for this hour...",
            ["mom_suspicious_4"] = "I'm awake-ish. Don't test me.",
            ["mom_almost_1"] = "Okay. That's it. Slippers... on.",
            ["mom_almost_2"] = "One... two...",
            ["mom_almost_3"] = "I am getting up. I am really getting up.",
            ["mom_wake_1"] = "Who is making all that noise?",
            ["mom_wake_2"] = "Do you know what time it is?",
            ["mom_wake_3"] = "I was dreaming about a quiet house!",
            ["mom_wake_4"] = "Nobody move. Mom is coming down.",
            ["mom_question_1"] = "Alright. Who did this? Show me.",
            ["mom_question_2"] = "Somebody saw something. Tell me.",
            ["mom_question_3"] = "I'll ask once. Who was it?",
            ["mom_investigate_1"] = "Let's see what we have here...",
            ["mom_investigate_2"] = "Something fell. Something loud.",
            ["mom_investigate_3"] = "Hmm. Footprints. Little ones.",
            ["mom_investigate_4"] = "This mess has a name on it.",
            ["mom_caught_1"] = "{name}! Bed. Now.",
            ["mom_caught_2"] = "{name}! Caught you! Off to bed!",
            ["mom_caught_3"] = "{name}! Really? Go to sleep.",
            ["mom_caught_4"] = "{name}! Aha! It was you!",
            ["mom_caught_5"] = "{name}! Pillow. Blanket. Now.",
            ["mom_penalty_1"] = "{name}! Gotcha! Fifteen seconds in bed, and those points are mine.",
            ["mom_penalty_2"] = "{name}! That was you. Sit on your bed and count to fifteen.",
            ["mom_penalty_3"] = "{name}! Busted! Timeout. Fifteen seconds.",
            ["mom_penalty_4"] = "{name}! I saw that. Points off, and a little nap for you.",
            ["mom_warning_1"] = "Huh. Just the pets. Fine. Back to bed.",
            ["mom_warning_2"] = "Nobody? Hmph. I'm watching you all.",
            ["mom_warning_3"] = "Must have been the wind. *yawn*",
            ["mom_warning_4"] = "Next time, somebody loses points.",
            ["mom_inspect_start_1"] = "Time's up! Let's see who actually helped.",
            ["mom_inspect_start_2"] = "Inspection time! Show me those jobs.",
            ["mom_inspect_start_3"] = "The clock says bedtime. Let's check the house.",
            ["mom_inspect_look_1"] = "Mm-hm. Mm-hm.",
            ["mom_inspect_look_2"] = "I see everything, you know.",
            ["mom_inspect_look_3"] = "Let me check my list.",
            ["mom_lazy_1"] = "{name}! You barely lifted a finger!",
            ["mom_lazy_2"] = "{name}! Least helpful tonight. Hmm.",
            ["mom_lazy_3"] = "{name}! Nice try, sweetie.",
            ["mom_lazy_4"] = "{name}! Those jobs didn't do themselves!",
            ["mom_tiebreak_1"] = "A tie? Fine! Quick challenge. Fastest helper wins. Go!",
            ["mom_bed_1"] = "Goodnight. And keep it down.",
            ["mom_bed_2"] = "*yawn* Don't make me come back out.",
            ["mom_bed_3"] = "Back to sleep... *snore*",
            ["mom_lastminute_1"] = "One more minute, and I'm checking everything.",
            ["mom_lastminute_2"] = "I hear the clock ticking out there...",
            ["mom_intro_1"] = "Listen up, my little night owls. Every one of you has jobs all over the house. Do yours, quietly. If I catch you, you lose points and take a little nap. When the clock runs out, the most helpful kid wins.",
            ["mom_intro_v5"] = "Listen up, night owls. Jobs in every room, one clock for everybody. Be quick, be quiet, and don't get caught.",
            ["mom_rush_1"] = "Somebody's finished already? The rest of you, hurry up.",
            ["mom_rush_2"] = "I hear somebody's done. Tick tock, little ones.",
            ["mom_sabotage_1"] = "Why are things moving around out there?",
            ["mom_sabotage_2"] = "I'd better not catch anybody hiding their sister's things.",
            ["mom_win"] = "{name} - the quietest, most helpful kid in this house. I'm so proud. Now everybody... sleep!",
        };

        public static bool HasName(string id) => L.TryGetValue(id, out var s) && s.Contains("{name}");
        public static bool Has(string id) => L.ContainsKey(id);

        public static string Text(string id, string name = null)
        {
            if (!L.TryGetValue(id, out var s)) return "";
            return name != null ? s.Replace("{name}", name) : s.Replace("{name}! ", "").Replace("{name} - ", "");
        }
    }
}
