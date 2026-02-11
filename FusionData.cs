[System.Serializable]
public class FusionData
{
    public Ability ability1;
    public Ability ability2;
    public Ability fusedAbility;

    public FusionData(Ability a1, Ability a2, Ability fused)
    {
        ability1 = a1;
        ability2 = a2;
        fusedAbility = fused;
    }
}