public sealed class ItemInstance
{
    public ItemDefinitionSO Definition { get; }
    public int StackCount { get; private set; }

    public ItemInstance(ItemDefinitionSO definition)
    {
        Definition = definition;
        StackCount = 1;
    }

    public void AddStack()
    {
        StackCount++;
    }
}
