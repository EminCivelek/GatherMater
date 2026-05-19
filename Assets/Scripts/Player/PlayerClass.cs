public enum PlayerClass { None, Warrior, Rogue, Wizard, Priest }

public static class ClassData
{
    public static float HPMult(PlayerClass c) => c switch
    {
        PlayerClass.Warrior => 1.20f,
        PlayerClass.Rogue   => 1.10f,
        PlayerClass.Priest  => 1.20f,
        _                   => 1.00f
    };

    public static float ArmorMult(PlayerClass c) => c switch
    {
        PlayerClass.Warrior => 1.30f,
        _                   => 1.00f
    };

    public static float AttackMult(PlayerClass c) => c switch
    {
        PlayerClass.Warrior => 1.10f,
        PlayerClass.Rogue   => 1.20f,
        PlayerClass.Wizard  => 1.40f,
        _                   => 1.00f
    };

    public static float AttackSpeedMult(PlayerClass c) => c switch
    {
        PlayerClass.Rogue  => 1.30f,
        PlayerClass.Wizard => 1.10f,
        _                  => 1.00f
    };

    public static float HPRegenMult(PlayerClass c) => c switch
    {
        PlayerClass.Priest => 2.00f,
        _                  => 1.00f
    };

    public static float MPRegenMult(PlayerClass c) => c switch
    {
        PlayerClass.Priest => 2.00f,
        _                  => 1.00f
    };

    public static float XPMult(PlayerClass c) => c switch
    {
        PlayerClass.Wizard => 1.25f,
        _                  => 1.00f
    };

    public static string DisplayName(PlayerClass c) => c switch
    {
        PlayerClass.Warrior => "Warrior",
        PlayerClass.Rogue   => "Rogue",
        PlayerClass.Wizard  => "Wizard",
        PlayerClass.Priest  => "Priest",
        _                   => "None"
    };

    public static string[] Passives(PlayerClass c) => c switch
    {
        PlayerClass.Warrior => new[] { "+20% Max HP", "+30% Armor", "+10% Attack Damage" },
        PlayerClass.Rogue   => new[] { "+30% Attack Speed", "+20% Attack Damage", "+10% Max HP" },
        PlayerClass.Wizard  => new[] { "+40% Attack Damage", "+25% XP Gain", "+10% Attack Speed" },
        PlayerClass.Priest  => new[] { "+100% HP Regen", "+100% MP Regen", "+20% Max HP" },
        _                   => System.Array.Empty<string>()
    };

    public static string PepTalk(PlayerClass c) => c switch
    {
        PlayerClass.Warrior =>
            "Steel forged in the fires of a thousand battles, unbroken by every blow — " +
            "you are the wall between darkness and the innocent.\n\n" +
            "Your might is not merely strength. It is a promise. A vow that no enemy " +
            "will pass while you still draw breath.\n\n" +
            "Rise, Warrior. These people need a champion.",

        PlayerClass.Rogue =>
            "The shadow does not fight — it decides when the fight is already over.\n\n" +
            "Speed is your shield. Precision is your sword. The darkness itself bows " +
            "to you as an ally. While others clash loudly, you have already won.\n\n" +
            "Strike first. Strike true. The world never sees the best rogues coming.",

        PlayerClass.Wizard =>
            "Power beyond mortal comprehension pulses through your veins. Every battle " +
            "is a theorem you solve with fire and force.\n\n" +
            "Your enemies do not fall to brute strength — they fall to the terrible, " +
            "elegant application of knowledge. Wisdom compounds; so does your mastery.\n\n" +
            "Study. Grow. Let your mind be the mightiest weapon this world has ever seen.",

        PlayerClass.Priest =>
            "Some fight to destroy — you fight to preserve.\n\n" +
            "Your faith is armor. Your devotion is the river from which allies drink " +
            "when they are parched. The greatest warriors have fallen; it was a Priest " +
            "who raised them again.\n\n" +
            "Be the light that does not go out.",

        _ => ""
    };
}
