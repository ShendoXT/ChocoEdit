﻿/*
 * Created by SharpDevelop.
 * User: suloku
 * Date: 01/03/2016
 * Time: 20:03
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ChocoEdit
{
    /// <summary>
    /// Description of MainForm.
    /// </summary>
    public partial class MainForm : Form
    {

        public MainForm(bool pluginEdit)
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
            //
            // The InitializeComponent() call is required for Windows Forms designer support.
            //
            InitializeComponent();
            pluginEditing = pluginEdit;

            //No need for lsz in psx plugin edit mode
            if (pluginEdit)
            {
                this.StartPosition = FormStartPosition.CenterParent;
                return;
            }

            string tempExeName = Path.Combine(Directory.GetCurrentDirectory(), "lzs.exe");

            ResourceManager resources = new ResourceManager("ChocoEdit.MainForm", Assembly.GetExecutingAssembly());
            byte[] exeData = (byte[])resources.GetObject("lzs");
            using (FileStream fsDst = new FileStream(tempExeName, FileMode.Create, FileAccess.Write))
            {
                fsDst.Write(exeData, 0, exeData.Length);
            }

            //
            // TODO: Add constructor code after the InitializeComponent() call.
            //
        }
        public string loadfilter = "PSXGameEdit single save or Chocorpg PC file|*.mcs;CHOCORPG;*.ff8|All Files (*.*)|*.*";
        public string mcsfilter = "PSXGameEdit single save|*.mcs|All Files (*.*)|*.*";
        public string chocorpgfilter = "CHOCORPG FF8 PC save file|CHOCORPG;*.ff8|All Files (*.*)|*.*";
        public byte[] savebuffer;
        public byte[] ff8buffer;
        public UInt32 ff8ID;
        public static Choco save;

        //True if save is being edited via plugin interface
        public bool pluginEditing = false;
        public bool saveAndClose = false;

        void MainScreenDragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }
        void MainScreenDragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            load_savegame(files[0]);
        }
        void Loadsave_butClick(object sender, EventArgs e)
        {
            load_savegame(null);
        }
        public static int compressor = 0;//0 lzs.exe, 1 qt-lzs.exe and unlzs.exe
        public static string pathTempfile = null;
        public static bool isCompressed = true;
        public void load_savegame(string filepath)
        {

            string path = filepath;
            int filesize = 0;

            if (pluginEditing)
                filesize = savebuffer.Count();
            else
                filesize = FileIO.load_file(ref savebuffer, ref path, loadfilter);

            versiontext.Text = "";

            if (filesize > 0)
            {
                savegamename.Text = path;
                save = new Choco(savebuffer, filesize);

                if (filesize == Choco.PSXSIZE) //PSX
                {
                    versiontext.Text = "PSX";
                    save_but.Enabled = true;
                    ff8id_set.Enabled = true;

                    load_data();
                }
                else //PC
                {
                    //Decompress if needed
                    if (save.isLzs == true)
                    {
                        isCompressed = true;
                        versiontext.Text = "PC";
                        string pathLZS;

                        if (compressor == 0)//use lzs.exe
                        {

                            pathLZS = Path.Combine(Directory.GetCurrentDirectory(), "lzs.exe");
                            pathTempfile = Path.Combine(Directory.GetCurrentDirectory(), "tempfile.ff8");

                            //Cleanup so we don't load a previous savefile if some error occurs
                            if (File.Exists(pathTempfile)) File.Delete(pathTempfile);

                            //Decompress
                            ProcessStartInfo startLZS = new ProcessStartInfo(pathLZS);
                            startLZS.WindowStyle = ProcessWindowStyle.Hidden;

                            startLZS.Arguments = "-d \"" + path + "\" \"" + pathTempfile + "\"";
                            var process = Process.Start(startLZS);
                            process.WaitForExit();
                            //
                            path = Path.Combine(Directory.GetCurrentDirectory(), "tempfile.ff8");
                        }
                        else if (compressor == 1)//Use qt-lzs
                        {
                            //If we are loading a new compressed file, delete previous decompressed one
                            if (pathTempfile != null)
                                if (File.Exists(pathTempfile)) File.Delete(pathTempfile);

                            pathLZS = "\"" + Path.Combine(Directory.GetCurrentDirectory(), "qt-lzs\\unlzs.exe") + "\"";
                            pathTempfile = Path.Combine(Path.Combine(Directory.GetCurrentDirectory(), "qt-lzs"), Path.GetFileName(path) + ".dec");

                            //Cleanup so we don't load a previous decomp savefile if some error occurs
                            if (File.Exists(pathTempfile)) File.Delete(pathTempfile);

                            //Decompress
                            ProcessStartInfo startLZS = new ProcessStartInfo(pathLZS);
                            startLZS.WindowStyle = ProcessWindowStyle.Hidden;

                            startLZS.Arguments = "\"" + path + "\" " + "\"" + Path.Combine(Directory.GetCurrentDirectory(), "qt-lzs") + "\"";
                            var process = Process.Start(startLZS);
                            process.WaitForExit();
                            //

                            path = pathTempfile;
                        }
                    }
                    else
                    {
                        isCompressed = false;
                        versiontext.Text = "PC (uncompressed)";
                    }

                    FileIO.load_file(ref savebuffer, ref path, null);

                    save = new Choco(savebuffer);
                    save_but.Enabled = true;
                    load_data();
                    //Remove *.dec file
                    if (File.Exists(path)) File.Delete(path);
                }

            }
            else
            {
                MessageBox.Show("Invalid file.");
                savegamename.Text = "";
                save_but.Enabled = false;
                ff8id_set.Enabled = false;
            }
        }
        void Save_butClick(object sender, EventArgs e)
        {
            save_data();

            if (pluginEditing)
            {
                saveAndClose = true;
                this.Close();
                return;
            }

            if (save.Data.Length == Choco.PSXSIZE)
            {
                FileIO.save_file(save.Data, mcsfilter);
            }
            else
            {
                string pathLZS;
                string pathTempfile2;
                string path;
                if (isCompressed == true) //Recompress
                {
                    if (compressor == 0) //lzs.exe
                    {
                        //Write edited data to temp file we will be compressing
                        FileIO.write_file(save.Data, Path.Combine(Directory.GetCurrentDirectory(), "tempfile.ff8"));

                        pathLZS = Path.Combine(Directory.GetCurrentDirectory(), "lzs.exe");
                        pathTempfile = Path.Combine(Directory.GetCurrentDirectory(), "tempfile.ff8");
                        pathTempfile2 = Path.Combine(Directory.GetCurrentDirectory(), "tempfle2.ff8");

                        //Compress
                        ProcessStartInfo startLZS = new ProcessStartInfo(pathLZS);
                        startLZS.WindowStyle = ProcessWindowStyle.Hidden;

                        startLZS.Arguments = "-c \"" + pathTempfile + "\" \"" + pathTempfile2 + "\"";
                        var process = Process.Start(startLZS);
                        process.WaitForExit();
                        //

                        path = Path.Combine(Directory.GetCurrentDirectory(), "tempfle2.ff8");
                        FileIO.load_file(ref savebuffer, ref path, null);
                        FileIO.save_file(savebuffer, chocorpgfilter);
                        //Remove tempfl2.ff8 after we saved
                        if (File.Exists(path)) File.Delete(path);
                    }
                    else if (compressor == 1) //qt-lzs.exe
                    {
                        //Write edited data to temp file we will be compressing
                        FileIO.write_file(save.Data, Path.Combine(Directory.GetCurrentDirectory(), "tempfile.ff8"));

                        pathLZS = "\"" + Path.Combine(Directory.GetCurrentDirectory(), "qt-lzs\\lzs.exe") + "\"";
                        string pathTempSave = Path.Combine(Directory.GetCurrentDirectory(), "tempfile.ff8");

                        //Compress
                        ProcessStartInfo startLZS = new ProcessStartInfo(pathLZS);
                        startLZS.WindowStyle = ProcessWindowStyle.Hidden;

                        startLZS.Arguments = "\"" + pathTempSave + "\" " + "\"" + Directory.GetCurrentDirectory() + "\"";
                        var process = Process.Start(startLZS);
                        process.WaitForExit();
                        //

                        //Remove temp uncompressed data
                        if (File.Exists(pathTempSave)) File.Delete(pathTempSave);

                        path = Path.Combine(Directory.GetCurrentDirectory(), "tempfile.ff8.lzs");
                        FileIO.load_file(ref savebuffer, ref path, null);
                        FileIO.save_file(savebuffer, chocorpgfilter);

                        //Remove .lzs after we saved
                        if (File.Exists(path)) File.Delete(path);
                    }
                }
                else
                    FileIO.save_file(savebuffer, chocorpgfilter);
            }
        }
        void SavegamenameTextChanged(object sender, EventArgs e)
        {

        }
        public void AboutClick(object sender, EventArgs e)
        {
            MessageBox.Show("Chocobo World Editor 0.2c by suloku"
                            + "\n\nThanks to:\n- Ortew Lant for his awesome Chocobo World guide and help with some offsets back in 2013.\n- Ficedula for his LZS (de)compressor."
                           );
        }
        void load_data()
        {
            if (save.HP_max > 99)
                hp_max.Value = 99;
            else if (save.HP_max < 6)
                hp_max.Value = 6;
            else
                hp_max.Value = save.HP_max;

            if (save.HP_cur > 99)
                hp_curr.Value = 99;
            else
                hp_curr.Value = save.HP_cur;

            level.Value = save.level;
            rank.Value = save.rank;
            powerups.Value = save.Powerups;

            //weapon_hi.Value = save.weapon_first;
            //weapon_lo.Value = save.weapon_last;
            weapon.Value = save.weapon;
            ID.Value = save.ID;

            ff8_id.Text = save.FF8ID.ToString("X");

            item_a.Value = save.ITEM_A;
            item_b.Value = save.ITEM_B;
            item_c.Value = save.ITEM_C;
            item_d.Value = save.ITEM_D;

            //Flags
            flag0.Checked = save.Eventflag0;
            flag1.Checked = save.Away;
            mogu_found.Checked = save.MoguFound;
            mogu_have.Checked = save.MoguHave;
            mogu_standby.Checked = save.MoguStandby;
            demon_defeat.Checked = save.DemonDefeated;
            lvlevent.Checked = save.LvlEvent;
            event_wait.Checked = save.EventWait;
        }
        void save_data()
        {
            save.HP_max = (int)hp_max.Value;
            save.HP_cur = (int)hp_curr.Value;

            save.level = (int)level.Value;
            save.rank = (int)rank.Value;
            save.Powerups = (int)powerups.Value;

            save.weapon = (int)weapon.Value;
            save.ID = (int)ID.Value;

            //ff8_id.Text = save.FF8ID.ToString("X");

            save.ITEM_A = (int)item_a.Value;
            save.ITEM_B = (int)item_b.Value;
            save.ITEM_C = (int)item_c.Value;
            save.ITEM_D = (int)item_d.Value;

            //Flags
            //save.Eventflag0 = flag0.Checked;
            //save.Away = flag1.Checked;
            save.MoguFound = mogu_found.Checked;
            save.MoguHave = mogu_have.Checked;
            save.MoguStandby = mogu_standby.Checked;
            save.DemonDefeated = demon_defeat.Checked;
            save.LvlEvent = lvlevent.Checked;
            save.EventWait = event_wait.Checked;
        }
        void MainFormLoad(object sender, EventArgs e)
        {
            if (pluginEditing)
            {
                loadsave_but.Enabled = false;
                savegamename.Enabled = false;
                save_but.Text = "Save and close";
            }
        }
        void Hp_currValueChanged(object sender, EventArgs e)
        {
            if (hp_curr.Value > hp_max.Value) hp_curr.Value = hp_max.Value;
        }
        void Ff8id_setClick(object sender, EventArgs e)
        {
            string path = null;
            int filesize = FileIO.load_file(ref ff8buffer, ref path, mcsfilter);
            versiontext.Text = "";

            if (filesize == 8320)
            {
                ff8ID = BitConverter.ToUInt32(ff8buffer, 0x1588);
                save.FF8ID = ff8ID;

                ff8_id.Text = save.FF8ID.ToString("X");

            }
            else
            {
                MessageBox.Show("Invalid file.");
            }
        }
        void About_rankClick(object sender, EventArgs e)
        {
            MessageBox.Show(
                "Rank---Max.----Max.-------Probability-of-item----#ID----ID" +
                "\n--##----HP---Weapon------A----B-----C-----D-----------Requirement" +
                "\n----------------------------------------------------------------------" +
                "\n--00-----41-----9999------25%--25%--25%--25%-----1---211" +
                "\n--01-----37-----9998-------5%--35%--30%--30%-----3---000,0008,777" +
                "\n--02-----35-----9898-------4%--10%--46%--40%-----8---All-digits-the-same" +
                "\n--03-----34-----9888-------3%--10%--37%--50%-----9---Ending-in-00" +
                "\n--04-----33-----9698-------2%--10%--28%--60%-----9---Ending-in-77" +
                "\n--05-----32-----9698-------1%---5%--25%--70%----90---Ending-in-7" +
                "\n--06-----31-----9698?------0%---5%--15%--80%---880---Any-other-number" +
                "\n\n In PC the probability is always A-3% B-10% C-37% D-50%" +
                "\n\nMax. HP is automatically set when you advance a level (so even if it is set to 99, after level up it will be set depending on rank)"
            );
        }
        void About_flagsClick(object sender, EventArgs e)
        {
            MessageBox.Show("About Level event happened:\n"
                + "Events happen at level 20, 50 (Boko PowerUp), 75, 100. I guess it is unset after every level up. Needs to be re-set to be able to battle the Demon King at level 100 (and Demon King defeated flag needs to be unset)"
                + "\n\nAbout Mogu found flag:"
                + "\nIn normal gameplay you can only encounter Mogu after reaching level 10. If you set this before level 10 you can encounter mogu even at level 1."
            );
        }
        void Hp_maxValueChanged(object sender, EventArgs e)
        {

        }

        static void OnProcessExit(object sender, EventArgs e)
        {
            //Remove tempfile.ff8 / *.dec when closing
            if (File.Exists(pathTempfile)) File.Delete(pathTempfile);
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "tempfile.ff8"))) File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "tempfile.ff8"));
        }
    }
}
