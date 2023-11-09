using System.Linq;
using System.Text;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Scripts
{
    public class Librarian : AmteMob
    {
        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;

            player.Client.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language,"Librarian.InteractText01"), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
            player.Client.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language,"Librarian.InteractText02"), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
            player.Client.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language,"Librarian.InteractText03"), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
            player.Client.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language,"Librarian.InteractText04"), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
            return true;
        }

        public override bool WhisperReceive(GameLiving source, string text)
        {
            var player = source as AmtePlayer;
            if (!base.WhisperReceive(source, text) || player == null)
                return false;

            switch (text)
            {
                case "Voir les livres":
                case "Consult the books":
                    player.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language,"Librarian.ResponseText01"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    StringBuilder sb = new StringBuilder(2048);
                    GameServer.Database.SelectObjects<DBBook>(b => b.IsInLibrary).OrderBy(b => b.Title).Foreach(
                        b =>
                        {
                            sb.Append("\n[").AppendLine(b.Title).Append("], ").Append(b.Author);
                            if (sb.Length > 1900)
                            {
                                player.SendMessage(sb.ToString(), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                                sb.Clear();
                            }
                        });
                    player.SendMessage(sb.ToString(), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    break;

                case "Ajouter un livre":
                case "Add a book":
                    player.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language,"Librarian.ResponseText02"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    break;

                default:
                    var book = GameServer.Database.SelectObject<DBBook>(b => b.Title == text && b.IsInLibrary);
                    if (book == null)
                    {
                        player.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language,"Librarian.ResponseText03"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                        break;
                    }
                    BooksMgr.ReadBook(player, book);
                    break;
            }
            return true;
        }

        public override bool ReceiveItem(GameLiving source, InventoryItem item)
        {
            var p = source as GamePlayer;
            if (p == null || item == null)
                return false;

            if (item.Id_nb.StartsWith("scroll"))
            {
                var book = GameServer.Database.SelectObject<DBBook>(b => b.Name == item.Name);
                if (book != null)
                {
                    if (book.PlayerID != p.InternalID)
                    {
                        p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language,"Librarian.ResponseText04"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                        return false;
                    }
                    book.IsInLibrary = !book.IsInLibrary;
                    book.Save();
                    p.Out.SendMessage(
                        book.IsInLibrary
                            ? "Votre livre fait maintenant partie de la bibliothèque."
                            : "Vous avez retiré votre livre de la bibliothèque.", eChatType.CT_System,
                        eChatLoc.CL_PopupWindow);
                }
                else
                    p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language,"Librarian.ResponseText05"), eChatType.CT_System, eChatLoc.CL_PopupWindow);

            }
            else
                p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language,"Librarian.ResponseText06"), eChatType.CT_System, eChatLoc.CL_PopupWindow);

            return false;
        }
    }
}
