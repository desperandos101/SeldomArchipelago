using Terraria.UI;

namespace SeldomDespArchipelago.UI
{
    class ArchipelagoUI : UIState
    {
        CollectionButton button;

        public override void OnInitialize()
        {
            button = new CollectionButton();
            Append(button);
        }
    }
}