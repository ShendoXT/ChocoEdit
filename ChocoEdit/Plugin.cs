using ChocoEdit;
using System.Windows.Forms;

namespace rexPluginSystem
{
    //All plugins should incorporate this interface in order to comunicate with MemcardRex 1.6+.
    public interface rexPluginInterfaceV2
    {
        string getPluginName();
        string getPluginAuthor();
        string getPluginSupportedGames();
        string[] getSupportedProductCodes();
        byte[] editSaveData(byte[] gameSaveData, string saveProductCode);
        void showAboutDialog();
        void showConfigDialog();
    }

    public class rexPlugin : rexPluginInterfaceV2
    {
        private const string pluginName = "ChocoEdit";
        private const string pluginVersion = "0.2c";
        private const string pluginAuthor = "suloku";
        private const string pluginSupportedGames = "ChocoboWorld (PocketStation)";

        //Return Plugin's name (name + plugin version is recommended)
        public string getPluginName()
        {
            return pluginName + " " + pluginVersion;
        }

        //Return Author's name.
        public string getPluginAuthor()
        {
            return pluginAuthor;
        }

        //Return a list of games supported by the plugin
        public string getPluginSupportedGames()
        {
            return pluginSupportedGames;
        }

        //Return a string array of product codes compatible with this plugin.
        //In order to make a product-code-independent-plugin one member should be "*.*".
        public string[] getSupportedProductCodes()
        {
            //All versions of Spyro the Dragon are supported
            return new string[] { "SLUSP00892" };
        }

        //A data to process. Edited save data should be returned.
        //Array size depends on the number of slots that specific save takes (Save header (128 bytes) + Number of slots * 8192 bytes).
        public byte[] editSaveData(byte[] gameSaveData, string saveProductCode)
        {
            if(gameSaveData.Length != Choco.PSXSIZE)
            {
                MessageBox.Show("This FF8 save does not contain Chocobo World", pluginName + " " + pluginVersion);
                return null;
            }

            MainForm mainForm = new MainForm(true);

            //Save data in mcs form, native to both MemcardRex plugins and ChocoEdit
            mainForm.savebuffer = gameSaveData;
            mainForm.load_savegame("MemcardRex");

            mainForm.ShowDialog();

            if (mainForm.saveAndClose) return mainForm.savebuffer;

            return null;
        }

        public void showAboutDialog()
        {
            new MainForm(true).AboutClick(this, null);
        }

        public void showConfigDialog()
        {
            MessageBox.Show("This plugin has no options you can configure.", pluginName + " " + pluginVersion);
        }
    }
}