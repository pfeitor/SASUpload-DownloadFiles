using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections;
using System.Reflection;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        static Form1()
        {
            ResourceExtractor.ExtractResourceToFile("WindowsFormsApplication1.SASInterop.dll", Path.GetTempPath()+"SASInterop.dll");
            ResourceExtractor.ExtractResourceToFile("WindowsFormsApplication1.SASOManInterop.dll", Path.GetTempPath()+"SASOManInterop.dll");
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //PopulateTreeView();
            textBox2.Text = Properties.Settings.Default.user;
            textBox3.Text = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(Properties.Settings.Default.pwd));

            //Resolve the embeded dlls from user's temp folder
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += new ResolveEventHandler(currentDomain_AssemblyResolve);
        }

        private Assembly currentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {

            string[] name = args.Name.Split(',');
            string path = System.IO.Path.Combine(Path.GetTempPath(), name[0] + ".dll");
            try
            {
                Assembly foundAssembly = Assembly.LoadFile(path);
                return foundAssembly;
            }
            catch (System.IO.FileNotFoundException ex)
            {
                throw new System.IO.FileNotFoundException("Could not load assembly from expected location", path, ex);
            }
        }
        
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (activeSession != null && activeSession.Workspace != null) activeSession.Workspace.Close();
            Properties.Settings.Default.user = textBox2.Text;
            Properties.Settings.Default.pwd = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(textBox3.Text));
            Properties.Settings.Default.Save();
        }

        #region Server File Browse
        
        public string size(long size)
        {
            if (size < KB)
            {
                return string.Format("{0} bytes", size);
            }
            else if (size < MB)
            {
                return string.Format("{0} KB", (size / KB));
            }
            else if (size < GB)
            {
                return string.Format("{0:0.00} MB", (size / MB));
            }
            else
            {
                return string.Format("{0:0.00} GB", (size / GB));
            }
        }

        const int KB = 1024;
        const int MB = 1048576;
        const int GB = 1073741824;

        private SasServer activeSession = null;



        private string listedPath = "";
        Array fieldInclusionMask = new bool[] { true, false, true, true, false, false };
        Array typenames, engines, modtimes, sizes;
        Array names = new String[] { };
        Array typecat = new String[] { };


        void LoadFoldersInTreeView(TreeView treeName)
        {
            if (activeSession != null && activeSession.Workspace != null)
            {
                activeSession.Workspace.FileService.ListFiles("/", SAS.FileServiceListFilesMode.FileServiceListFilesModeRoot, ref fieldInclusionMask, out listedPath, out names, out typenames, out typecat,
                out sizes, out modtimes, out engines);
            }

            treeName.BeginUpdate();
 
            TreeNode node = new TreeNode(listedPath);
            node.ImageIndex = 0;
            node.Tag = new string[] {listedPath,"","O"};
            GetDirectories(node, listedPath);
            //GetFiles(node, listedPath);
            treeView1.Nodes.Add(node);

            treeName.EndUpdate();

        }
                
        void GetFiles(TreeNode node, string pth)
        {
            TreeNode treeNode1 = new TreeNode();
            if (activeSession != null && activeSession.Workspace != null)
            {
                activeSession.Workspace.FileService.ListFiles(pth, SAS.FileServiceListFilesMode.FileServiceListFilesModePath, ref fieldInclusionMask, out listedPath, out names, out typenames, out typecat,
                out sizes, out modtimes, out engines);
            }
            for (int i = 0; i < names.Length; i++)
            {
                if (typecat.GetValue(i).ToString() == "FileServiceTypeCategoryDirectory")
                {
//do nothing;
                }
                else 
                {
                    treeNode1 = node.Nodes.Add(names.GetValue(i).ToString());
                    string prepth = pth + "/" + names.GetValue(i).ToString();
                    treeNode1.Tag = new string[] {prepth.Replace("\\", "/").Replace("//","/"), sizes.GetValue(i).ToString(),"O"};
                    treeNode1.ImageIndex = 1;
                    treeNode1.SelectedImageIndex = 1;
                    //treeNode1.ToolTipText = "File Size:" + (Convert.ToDouble(sizes.GetValue(i).ToString())/1024).ToString("N2") + " KB";
                    treeNode1.ToolTipText = "File Size:" + size((int)sizes.GetValue(i));
                }

            }

 
        }

        void GetDirectories(TreeNode node, string pth)
        {
            TreeNode treeNode1 = new TreeNode();
            if (activeSession != null && activeSession.Workspace != null)
            {
                activeSession.Workspace.FileService.ListFiles(pth, SAS.FileServiceListFilesMode.FileServiceListFilesModePath, ref fieldInclusionMask, out listedPath, out names, out typenames, out typecat,
                out sizes, out modtimes, out engines);
            }
            for (int i = 0; i < names.Length; i++)
            {
                if (typecat.GetValue(i).ToString() == "FileServiceTypeCategoryDirectory")
                {
                    string prepth = pth + "/" + names.GetValue(i).ToString();
                    string ppth = prepth.Replace("\\", "/").Replace("//", "/");
                    string ut = "/users/"+textBox2.Text;

                    if (ppth.Equals("/sas") || ppth.Equals("/users") || ppth.Contains("/sas/amlrules") || ppth.Contains("/sas/amlfiutools") || ppth.Contains(ut))
                    {
                    treeNode1 = node.Nodes.Add(names.GetValue(i).ToString());
                    treeNode1.Nodes.Add(new TreeNode());
                    treeNode1.Tag = new string[] {ppth,sizes.GetValue(i).ToString(),"O"};
                    treeNode1.ImageIndex = 0;
                    treeNode1.SelectedImageIndex = 0;

                    }
                    else { continue; }
                }
                else
                {
//do nothing;
                }

            }

        }
        
        void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Parent !=null )treeView1.SelectedNode = e.Node;

            try
            {
                string d = e.Node.FullPath;
                string dir = d.Replace("\\", "/").Replace("//", "/");
                string[] tag = (string[])e.Node.Tag;

                if ((e.Node.Nodes.Count > 1 && tag[2]=="O") || !e.Node.IsExpanded) { /*Do Nothing.*/ } else { e.Node.Nodes.Clear();  GetDirectories(e.Node, dir); GetFiles(e.Node, dir); e.Node.Expand(); }
              
             }
            catch
            {
            }
        }
                
        private void button3_Click(object sender, EventArgs e)
        {
            if (activeSession != null && activeSession.Workspace != null) MessageBox.Show("Already connected", "Reminder");
            activeSession = new SasServer();
            if (textBox2.Text == String.Empty || textBox3.Text == String.Empty) { MessageBox.Show("Please enter User/Password.", "Reminder"); }
            else
            {
                activeSession.UserId = textBox2.Text;
                activeSession.Password = textBox3.Text;

                try
                {
                    activeSession.Connect();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                finally { LoadFoldersInTreeView(treeView1); }
            }
        }

        private List<string> showfolders = new List<string>();

        private void button2_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                showfolders.Add(folderBrowserDialog1.SelectedPath);
                this.treeView2.SelectedNodes.Clear();
                this.treeView2.Nodes.Clear();
                PopulateTreeView();
            }            
        }

        //upload small file
        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = openFileDialog1.FileName;
                FileInfo f = new FileInfo(textBox1.Text);
                //textBox1.Tag = f.Length;

                //int fs = Convert.ToInt32(textBox1.Tag.ToString());

                if (f.Length > 0 && f.Length < MB && activeSession != null && activeSession.Workspace != null)
                {
                    if (treeView1.SelectedNode == null || (treeView1.SelectedNode.Nodes.Count == 0 && treeView1.SelectedNode.SelectedImageIndex == 1)) { MessageBox.Show("Please select the folder.", "Reminder"); }
                    else
                    {


                        TreeNode node = this.treeView1.SelectedNode;
                        string upath = node.FullPath.Replace("\\", "/") + "/" + Path.GetFileName(textBox1.Text);

                        string upldfileref = "";
                        if (this.binaryToolStripMenuItem.Checked)
                        {
                            SAS.BinaryStream stm = activeSession.Workspace.FileService.AssignFileref("", "DISK", upath, "", out upldfileref).OpenBinaryStream(SAS.StreamOpenMode.StreamOpenModeForWriting);

                            try
                            {
                                Array toupld = File.ReadAllBytes(textBox1.Text);
                                stm.Write(ref toupld);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message);
                            }
                            finally
                            {
                                if (stm != null) stm.Close();
                            }
                        }
                        else
                        {
                            SAS.TextStream stm = activeSession.Workspace.FileService.AssignFileref("", "DISK", upath, "", out upldfileref).OpenTextStream(SAS.StreamOpenMode.StreamOpenModeForWriting,1024);

                            try
                            {
                                String toupld = File.ReadAllText(textBox1.Text).Replace("\r\n", "\n"); 
                                stm.Write(toupld);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message);
                            }
                            finally
                            {
                                if (stm != null) stm.Close();
                            }
                        }

                    }
                }
                else { MessageBox.Show("Only intend for uploading small file.", "Reminder"); }
            }

        }

        private void button4_Click(object sender, EventArgs e)
        {
            treeView1.SelectedNodes.Clear();
            treeView1.Nodes.Clear();
            LoadFoldersInTreeView(treeView1);
        }
         
        #endregion


        #region Local File browser
        private void PopulateTreeView()
   
        {
            foreach(string rootPath in showfolders)
            {
       TreeNode rootNode;
       //string rootPath = "d:\\data\\" + Environment.UserName;

       DirectoryInfo info = new DirectoryInfo(rootPath);
            if (info.Exists)
       
            {
           rootNode = new TreeNode(info.FullName);
           rootNode.ImageIndex = 0;
           rootNode.Tag = new string[] {rootPath,"","O"};
           GetDirectories(info.GetDirectories(), rootNode);
           GetFiles(info.GetFiles(), rootNode);
           treeView2.Nodes.Add(rootNode);
            }
            }
        }

    private void GetDirectories(DirectoryInfo[] subDirs, TreeNode nodeToAddTo)
    {
       TreeNode dNode;

       foreach (DirectoryInfo subDir in subDirs)
       {
           dNode = new TreeNode(subDir.Name, 0, 0);
           dNode.Tag = new string[] {"Dir","","O"};
           nodeToAddTo.Nodes.Add(dNode);
           dNode.Nodes.Add(new TreeNode());
       }
   }

    private void GetFiles(FileInfo[] files, TreeNode nodeToAddTo)
    {
        TreeNode aNode;
        foreach (FileInfo file in files)
        {
            aNode = new TreeNode(file.Name, 1, 1);
            aNode.Tag = new string[] {"File",file.Length.ToString(),"O"};
            aNode.ToolTipText = "File Size:" + size(file.Length);
            nodeToAddTo.Nodes.Add(aNode);
        }
    }

    void treeView2_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
    {

        treeView2.SelectedNode = e.Node;

        string[] tag = (string[])e.Node.Tag;

        try
        {
            DirectoryInfo d = new DirectoryInfo(@e.Node.FullPath);
            if ((e.Node.Nodes.Count > 1 && tag[2] == "O") || !e.Node.IsExpanded) { /*Do Nothing.*/ } else { e.Node.Nodes.Clear();  GetDirectories(d.GetDirectories(), e.Node); GetFiles(d.GetFiles(), e.Node); e.Node.Expand(); }
        }
        catch (Exception ex)
        {

            if (ex is System.NullReferenceException || ex is System.UnauthorizedAccessException)
            {
            }

        }
    }
        #endregion

        #region Drag/Drop 


    private void treeView1_ItemDrag(object sender, ItemDragEventArgs e)
    {
        // Move the dragged node when the left mouse button is used. 
        //if (e.Button == MouseButtons.Left)
        //{
        //    DoDragDrop(e.Item, DragDropEffects.Move);
        //}

        // Copy the dragged node when the right mouse button is used. 
        //else if (e.Button == MouseButtons.Right)
        //{
        if (((TreeViewMS.TreeViewMS)sender).SelectedNodes.Count == 0 || (((TreeViewMS.TreeViewMS)sender).SelectedNodes.Count == 1 && ((TreeViewMS.TreeViewMS)sender).SelectedNodes[0] != ((TreeNode)e.Item)))
        {
            treeView1.SelectedNode = (TreeNode)e.Item;
            ((TreeViewMS.TreeViewMS)sender).SelectedNodes.Clear();
            ((TreeViewMS.TreeViewMS)sender).SelectedNodes.Add((TreeNode)e.Item);
        }

        DoDragDrop(((TreeViewMS.TreeViewMS)sender).SelectedNodes, DragDropEffects.Copy);

        //}
    }

    private void treeView2_ItemDrag(object sender, ItemDragEventArgs e)
    {
        // Move the dragged node when the left mouse button is used. 
        //if (e.Button == MouseButtons.Left)
        //{
        //    DoDragDrop(e.Item, DragDropEffects.Move);
        //}

        // Copy the dragged node when the right mouse button is used. 
        //else if (e.Button == MouseButtons.Right)
        //{
        //DoDragDrop(e.Item, DragDropEffects.Copy);
        if (((TreeViewMS.TreeViewMS)sender).SelectedNodes.Count == 0 || (((TreeViewMS.TreeViewMS)sender).SelectedNodes.Count == 1 && ((TreeViewMS.TreeViewMS)sender).SelectedNodes[0] != ((TreeNode)e.Item)))
        {
            treeView2.SelectedNode = (TreeNode)e.Item;
            ((TreeViewMS.TreeViewMS)sender).SelectedNodes.Clear();
            ((TreeViewMS.TreeViewMS)sender).SelectedNodes.Add((TreeNode)e.Item);
        }

        DoDragDrop(((TreeViewMS.TreeViewMS)sender).SelectedNodes, DragDropEffects.Copy);
        //}
    }

    private void treeView2_DragEnter(object sender, DragEventArgs e)
    {
        e.Effect = e.AllowedEffect;
    }

    private void treeView1_DragEnter(object sender, DragEventArgs e)
    {
        e.Effect = e.AllowedEffect;
    }

        Queue<List<object>> _queuedDls = new Queue<List<object>>(); 
        Queue<List<object>> _queuedUps = new Queue<List<object>>();

//download
    private void treeView2_DragDrop(object sender, DragEventArgs e)
    {
        Point targetPoint = treeView2.PointToClient(new Point(e.X, e.Y));

        TreeNode targetNode = treeView2.GetNodeAt(targetPoint);

        //TreeNode draggedNode = (TreeNode)e.Data.GetData(typeof(TreeNode));
        ArrayList draggedNodes = e.Data.GetData(e.Data.GetFormats()[0]) as ArrayList;

        //MessageBox.Show(draggedNodes[1].ToString());

        foreach (TreeNode draggedNode in draggedNodes)
        {
             //&& targetNode.Parent != null
            if (!draggedNode.Equals(targetNode) && !FindRootNode(draggedNode).Equals(FindRootNode(targetNode)) && draggedNode.ImageIndex == 1 && targetNode != null)
            {

                if (e.Effect == DragDropEffects.Move)
                {
                    draggedNode.Remove();
                    targetNode.Nodes.Add(draggedNode);
                }

                else if (e.Effect == DragDropEffects.Copy)
                {
                    TreeNode dg = (TreeNode)draggedNode.Clone();
                    dg.BackColor = targetNode.TreeView.BackColor;
                    dg.ForeColor = targetNode.TreeView.ForeColor;
                    targetNode.Nodes.Add(dg);
                    //targetNode.Nodes.Add((TreeNode)draggedNode.Clone());

                }


                if (draggedNode.ImageIndex == 1 && targetNode.ImageIndex == 0)
                {
                    this.progressBar1.Visible = true;

                    List<object> arguments = new List<object>();
                    arguments.Add(draggedNode);
                    arguments.Add(targetNode);
                    
                    _queuedDls.Enqueue(arguments);
                }

            }
        }
        rundlQue();
    }

    private void rundlQue()
    {
        if (_queuedDls.Count != 0)
        {
            List<object> curr = _queuedDls.Dequeue();

            if (!backgroundWorker2.IsBusy && curr != null)
            {
                backgroundWorker2.RunWorkerAsync(curr); //MessageBox.Show(curr.Capacity.ToString()); 
            }
        }
    }


//upload
    private void treeView1_DragDrop(object sender, DragEventArgs e)
    {
        Point targetPoint = treeView1.PointToClient(new Point(e.X, e.Y));

        TreeNode targetNode = treeView1.GetNodeAt(targetPoint);
        ArrayList draggedNodes = e.Data.GetData(e.Data.GetFormats()[0]) as ArrayList;

        //MessageBox.Show(draggedNodes[1].ToString());

        foreach (TreeNode draggedNode in draggedNodes)
        {
             //&& targetNode.Parent != null
            if (!draggedNode.Equals(targetNode) && !FindRootNode(draggedNode).Equals(FindRootNode(targetNode)) && draggedNode.ImageIndex == 1 && targetNode != null)
            {
                if (e.Effect == DragDropEffects.Move)
                {
                    //draggedNode.Remove();
                    targetNode.Nodes.Add(draggedNode);
                }

                else if (e.Effect == DragDropEffects.Copy)
                {
                    TreeNode dg = (TreeNode)draggedNode.Clone();
                    dg.BackColor = targetNode.TreeView.BackColor;
                    dg.ForeColor = targetNode.TreeView.ForeColor;
                    targetNode.Nodes.Add(dg);
                }

                if (draggedNode.ImageIndex == 1 && File.Exists(draggedNode.FullPath) && targetNode.ImageIndex == 0)
                {

                    this.progressBar1.Visible = true;
                    List<object> arguments = new List<object>();
                    arguments.Add(draggedNode);
                    arguments.Add(targetNode);
                    
                    _queuedUps.Enqueue(arguments);
                    
                    //backgroundWorker1.RunWorkerAsync(arguments);
                }
            }
        }
        runupQue();
    }

    private void runupQue()
    {
        if (_queuedUps.Count != 0)
        {
            List<object> curr = _queuedUps.Dequeue();

            if (!backgroundWorker1.IsBusy && curr != null)
            {
                backgroundWorker1.RunWorkerAsync(curr); //MessageBox.Show(curr.Capacity.ToString()); 
            }
        }
    }

    private TreeNode FindRootNode(TreeNode treeNode)
    {
        if (treeNode == null) { return treeNode; }
        else
        {
            while (treeNode.Parent != null)
            {
                treeNode = treeNode.Parent;
            } 
            return treeNode;
        }
    }
        #endregion

        #region Rename/Preview/Delete/CreatDir/Convert LineBreak

//local refresh
    private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
    {
        string[] tag = (string[])treeView2.SelectedNode.Tag;
        treeView2.SelectedNode.Tag = new string[] { tag[0], tag[1], "A" };
        treeView2.SelectedNode.Collapse();

    }

//Delete button
    private void button6_Click(object sender, EventArgs e)
    {
        if (treeView1.SelectedNodes == null) MessageBox.Show("Please select the file/folder!", "Reminder");
        else
        {

            //for (int i = 0; i < treeView1.SelectedNodes.Count-1; i++ )
            foreach(TreeNode tn in treeView1.SelectedNodes)
            {
                 //TreeNode tn = (TreeNode)treeView1.SelectedNodes[i];

                string[] tag = (string[])tn.Tag;

                string pth = tag[0];

                if (activeSession != null && activeSession.Workspace != null)
                {
                    try
                    {
                        activeSession.Workspace.FileService.DeleteFile(pth);

                    }
                    catch (Exception ex)
                    {
                        Match match = Regex.Match(ex.Message, "\">.*</SAS");
                        MessageBox.Show(match.Value.Replace("</SAS", string.Empty).Replace("\">", string.Empty));
                        //MessageBox.Show(ex.Message);
                    }
                }
            }
            
            treeView1.SelectedNodes.Clear();
            if (treeView1.SelectedNode != null)
            {
                treeView1.SelectedNode.Parent.Collapse();
                string[] ctag = (string[])treeView1.SelectedNode.Tag;
                treeView1.SelectedNode.Tag = new string[] { ctag[0], ctag[1], "A" };
            }

          }
    }

//CreateDir
    private void button5_Click(object sender, EventArgs e)
    {
        if (treeView1.SelectedNode == null) MessageBox.Show("Please select the file/folder!", "Reminder");
        else if (treeView1.SelectedNode.SelectedImageIndex == 0)
        {
            string[] tag = (string[])treeView1.SelectedNode.Tag;
            string pth = tag[0];
            //int p = treeView1.SelectedNode.Parent.Index;
            if (activeSession != null && activeSession.Workspace != null)
            {
                try
                {
                    activeSession.Workspace.FileService.MakeDirectory(pth, "New");
                    TreeNode cd = new TreeNode("New");
                    cd.Tag = new string[] { pth + "/New", "", "O" };
                    treeView1.SelectedNode.Nodes.Add(cd);
                    //refresh();

                }
                catch (Exception ex)
                {
                    Match match = Regex.Match(ex.Message, "\">.*</SAS");
                    MessageBox.Show(match.Value.Replace("</SAS", string.Empty).Replace("\">", string.Empty));
                }
                finally { treeView1.SelectedNode.Tag = new string[] { tag[0], tag[1], "A" }; }
            }

        }

    }

//Rename
    private delegate void rnm();

    private void treeView1_AfterLabelEdit(object sender, System.Windows.Forms.NodeLabelEditEventArgs e)
    {

          if (e.Label != null)
        {
            if (e.Label.Length > 0)
            {
                if (e.Label.IndexOfAny(new char[] { '@', ',', '!' }) == -1)
                {
                    // Stop editing without canceling the label change.
                    e.Node.EndEdit(false);

                    rnm r = new rnm(() => afterAfterEdit(e.Node));
                    this.BeginInvoke(r);

                }
                else
                {
                    /* Cancel the label edit action, inform the user, and 
                       place the node in edit mode again. */
                    e.CancelEdit = true;
                    MessageBox.Show("Invalid file/directory name.\n" +
                       "The invalid characters are: '@',',','!'",
                       "Rename");
                    e.Node.BeginEdit();
                }
            }
            else
            {
                /* Cancel the label edit action, inform the user, and 
                   place the node in edit mode again. */
                e.CancelEdit = true;
                MessageBox.Show("Invalid file/directory name.\nThe name cannot be blank",
                   "Rename");
                e.Node.BeginEdit();
            }
        }
    }


   private void afterAfterEdit(TreeNode node)
   {
       string[] tag = (string[])node.Tag;
       string oldpth = tag[0];
       string fsize = tag[1];
     

       string[] ntag = (string[])node.Parent.Tag;
       string newpth = ntag[0] + "/" + node.Text;
       //MessageBox.Show(oldpth, newpth);
       if (activeSession != null && activeSession.Workspace != null)
       {
           try
           {
               activeSession.Workspace.FileService.RenameFile(oldpth, newpth);
           }
           catch (Exception ex)
           {
               Match match = Regex.Match(ex.Message, "\">.*</SAS");
               MessageBox.Show(match.Value.Replace("</SAS", string.Empty).Replace("\">", string.Empty));
           }
           finally
           {
               node.Tag = new string[]{newpth,tag[1],tag[2]};
           }
       }
   }

        //local delete
   private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
   {
       
       if (treeView2.SelectedNode != null)
       {
           try
           {
               File.Delete(@treeView2.SelectedNode.FullPath);
               treeView2.SelectedNode.Remove();
           }
           catch(Exception ex)
           {
               MessageBox.Show(ex.Message);
           }
           finally { treeView2.SelectedNodes.Clear(); }
       }
       else return;
   }
        //server delete
   private void deleteToolStripMenuItem1_Click(object sender, EventArgs e)
   {
       if (treeView1.SelectedNode == null) MessageBox.Show("Please select the file/folder!", "Reminder");
       else
       {
           string[] tag = (string[])treeView1.SelectedNode.Tag;
           string pth = tag[0];
           //int p = treeView1.SelectedNode.Parent.Index;
           if (activeSession != null && activeSession.Workspace != null)
           {
               try
               {
                   activeSession.Workspace.FileService.DeleteFile(pth);
                   treeView1.SelectedNode.Remove();
               }
               catch (Exception ex)
               {
                   Match match = Regex.Match(ex.Message, "\">.*</SAS");
                   MessageBox.Show(match.Value.Replace("</SAS", string.Empty).Replace("\">", string.Empty));
               }
               finally { treeView1.SelectedNodes.Clear(); }

           }
       }
   }

   void treeView2_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
   {
       try
       {
           if (e.Node.ImageIndex==1 && e.Node.Nodes.Count==0)
               System.Diagnostics.Process.Start(@e.Node.FullPath);
       }
       catch (Exception ex)
       {
           MessageBox.Show(ex.Message);
       }
   }

   void treeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
   {
       try
       {
           if (e.Node.ImageIndex == 1 && e.Node.Nodes.Count == 0)
           {
               string tempfile = Path.GetTempPath() + "\\" + e.Node.Text;
               string dldfileref = "";
               Array todld;
                SAS.BinaryStream stm = null;
                int byteCount = 0;
                bool endOfFile = false;
                string upath = e.Node.FullPath.Replace("\\", "/").Replace("//", "/");
                FileStream fStream = null;
                fStream = new FileStream(tempfile, System.IO.FileMode.Create);

                try
                {
                     stm = activeSession.Workspace.FileService.AssignFileref("", "DISK", upath, "", out dldfileref).OpenBinaryStream(SAS.StreamOpenMode.StreamOpenModeForReading);

                     for (int i = 0; i < 500; i++ )
                     {
                         stm.Read(KB, out todld);
                         fStream.Write((byte[])todld, 0, todld.Length);

                         endOfFile = (todld.Length < KB);
                         if (endOfFile) break;

                         byteCount = byteCount + todld.Length; //filesize

                     } 
                   if (stm != null) stm.Close(); 
                }
                catch { }
                finally 
                {
                    //activeSession.Workspace.FileService.DeassignFileref(dldfileref);
                   if (fStream != null)  fStream.Close();
                }
                if (endOfFile) System.Diagnostics.Process.Start(tempfile);
                else MessageBox.Show("No preview for big file.");
           }
       }
       catch (Exception ex)
       {
           MessageBox.Show(ex.Message);
       }
   }

//Convert Line Break
   private void convtoolStripMenuItem_Click(object sender, EventArgs e)
   {
       string text = "";
       try
       {
           if (treeView2.SelectedNode != null && treeView2.SelectedNode.ImageIndex == 1 && treeView2.SelectedNode.Nodes.Count == 0)                 
               text= File.ReadAllText(@treeView2.SelectedNode.FullPath).Replace("\r","\n").Replace("\n", "\r\n");
               File.WriteAllText(@treeView2.SelectedNode.FullPath, text);
       }
       catch (Exception ex)
       {
           MessageBox.Show(ex.Message);
       }

   }

   private void aboutToolStripMenuItem1_Click(object sender, EventArgs e)
   {
       if (!CheckOpened("About"))
       {
           Form3 frm = new Form3();
           frm.Owner = this;
           frm.Show();
       }
       //(new Form3()).Show();
   }

   private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
   {
       if (!CheckOpened("About"))
       {
           Form3 frm = new Form3();
           frm.Owner = this;
           frm.Show();
       }
       //(new Form3()).Show();

   }

   private bool CheckOpened(string name)
   {
       FormCollection fc = Application.OpenForms;

       foreach (Form frm in fc)
       {
           if (frm.Text == name)
           {
               return true;
           }
       }
       return false;
   }

     #endregion

   
        #region background work on download/upload
//upload
   private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
   {
       BackgroundWorker worker = sender as BackgroundWorker;
       List<object> tnd = e.Argument as List<object>;
       TreeNode draggedNode = (TreeNode)tnd[0];
       TreeNode targetNode = (TreeNode)tnd[1];
       SAS.TextStream tstm = null;
       SAS.BinaryStream bstm = null;
       FileStream fStream = null;
       string upldfileref = "";
       string[] tag = (string[])draggedNode.Tag;
       int per = 0;

       string[] ttag = (string[])targetNode.Tag;

       targetNode.Tag = new string[] { ttag[0], ttag[1], "A" };

       try
       {
           Array toupld;
           string upath = targetNode.FullPath.Replace("\\", "/") + "/" + draggedNode.Text;
           string pth = upath.Replace("//", "/");

           if (this.binaryToolStripMenuItem.Checked)
           {
               bstm = activeSession.Workspace.FileService.AssignFileref("", "DISK", pth, "", out upldfileref).OpenBinaryStream(SAS.StreamOpenMode.StreamOpenModeForWriting);

               int totalBytes = Convert.ToInt32(tag[1]);

               using (fStream = new FileStream(@draggedNode.FullPath, FileMode.Open, FileAccess.Read))
                   for (int c = 0; c <= totalBytes / MB; c++)
                   {
                       if (totalBytes < MB)
                       {
                           toupld = new byte[totalBytes];
                           fStream.Read((byte[])toupld, 0, totalBytes);
                           bstm.Write(ref toupld);
                       }

                       else if (totalBytes > MB * (c + 1))
                       {
                           toupld = new byte[MB];
                           fStream.Read((byte[])toupld, 0, MB);
                           bstm.Write(ref toupld);
                       }

                       else
                       {
                           toupld = new byte[totalBytes - MB * c];
                           fStream.Read((byte[])toupld, 0, totalBytes - MB * c);
                           bstm.Write(ref toupld);
                       }

                       float pp = (float)c * MB / totalBytes;
                       per = Math.Abs((int)(pp * 100));

                       //using (StreamWriter sw = File.AppendText(@"d:\data\541395034\Desktop\Work\up.txt"))
                       //{ sw.WriteLine(pp.ToString() + "\t" + totalBytes.ToString() + "\t" + per.ToString()); }

                       worker.ReportProgress(per);
                   }
           }

           else
           {
               tstm = activeSession.Workspace.FileService.AssignFileref("", "DISK", pth, "", out upldfileref).OpenTextStream(SAS.StreamOpenMode.StreamOpenModeForWriting,1024);

               int totalBytes = Convert.ToInt32(tag[1]);

               using (fStream = new FileStream(@draggedNode.FullPath, FileMode.Open, FileAccess.Read))
                   for (int c = 0; c <= totalBytes / MB; c++)
                   {
                       if (totalBytes < MB)
                       {
                           toupld = new byte[totalBytes];
                           fStream.Read((byte[])toupld, 0, totalBytes);
                           string toupldtxt = System.Text.Encoding.UTF8.GetString((byte[])toupld).Replace("\r\n","\n");
                           tstm.Write(toupldtxt);
                       }

                       else if (totalBytes > MB * (c + 1))
                       {
                           toupld = new byte[MB];
                           fStream.Read((byte[])toupld, 0, MB);
                           string toupldtxt = System.Text.Encoding.UTF8.GetString((byte[])toupld).Replace("\r\n", "\n");
                           tstm.Write(toupldtxt);
                       }

                       else
                       {
                           toupld = new byte[totalBytes - MB * c];
                           fStream.Read((byte[])toupld, 0, totalBytes - MB * c);
                           string toupldtxt = System.Text.Encoding.UTF8.GetString((byte[])toupld).Replace("\r\n", "\n");
                           tstm.Write(toupldtxt);
                       }

                       float pp = (float)c * MB / totalBytes;
                       per = Math.Abs((int)(pp * 100));

                       //using (StreamWriter sw = File.AppendText(@"d:\data\541395034\Desktop\Work\up.txt"))
                       //{ sw.WriteLine(pp.ToString() + "\t" + totalBytes.ToString() + "\t" + per.ToString()); }

                       worker.ReportProgress(per);
                   }
           }
       }
       catch { }
       finally
       {
           if (fStream != null) fStream.Close();
           if (bstm !=null) bstm.Close();
           if (tstm != null) tstm.Close();
       }
       e.Result = tag[1];
   }

   private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
   {     
       this.progressBar1.Visible = false;
       int size = Convert.ToInt32(e.Result.ToString());
       string fs = "";
       if (size < KB)
       {
           fs = string.Format("{0} bytes", size);
       }
       else if (size < MB)
       {
           fs = string.Format("{0} KB", (size / KB));
       }
       else if (size < GB)
       {
           fs = string.Format("{0:0.00} MB", (size / MB));
       }
       else
       {
           fs = string.Format("{0:0.00} GB", (size / GB));
       }
       MessageBox.Show(fs + " Uploaded");
       runupQue();

   }

   private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
   {
       this.progressBar1.Visible = true;
       this.progressBar1.Value = e.ProgressPercentage;    
   }

//download
   private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
   {
       BackgroundWorker worker = sender as BackgroundWorker;
       List<object> tnd = e.Argument as List<object>;
       TreeNode draggedNode = (TreeNode)tnd[0];
       TreeNode targetNode = (TreeNode)tnd[1];

       Array todld;
       SAS.BinaryStream stm = null;
       string[] tag = (string[])draggedNode.Tag;
       int byteCount = 0;
       int totalBytes = Convert.ToInt32(tag[1]);
       bool endOfFile = false;
       string dldfileref = "";
       string upath = draggedNode.FullPath.Replace("\\", "/").Replace("//", "/");

       FileStream fStream = null;
   
       fStream = new FileStream(targetNode.FullPath + "\\" + draggedNode.Text, FileMode.Create);
       //MessageBox.Show(targetNode.FullPath);
       int per = 0;


       string[] ttag = (string[])targetNode.Tag;

       targetNode.Tag = new string[] { ttag[0], ttag[1], "A" };

       try
       {
           stm = activeSession.Workspace.FileService.AssignFileref("", "DISK", upath, "", out dldfileref).OpenBinaryStream(SAS.StreamOpenMode.StreamOpenModeForReading);

           do
           {

               stm.Read(MB, out todld);
               fStream.Write((byte[])todld, 0, todld.Length);
               //FileSystem.WriteAllBytes(targetNode.FullPath + "\\" + draggedNode.Text,(byte[])todld,true);
               endOfFile = (todld.Length < MB);

               byteCount = byteCount + todld.Length; //filesize

               float pp = (float) byteCount / totalBytes;
               per = Math.Abs((int)(pp*100));

               //using (StreamWriter sw = File.AppendText(@"d:\data\541395034\Desktop\Work\dl.txt"))
               //{ sw.WriteLine(byteCount.ToString() + "\t" + totalBytes.ToString() + "\t" + per.ToString()); }

               worker.ReportProgress(per);

           } while (!endOfFile);

           //File.WriteAllBytes(targetNode.FullPath + "\\" + draggedNode.Text, (byte[])todld);
           if (stm != null )stm.Close();
       }
       catch { }
       finally
       {
           if(fStream != null) fStream.Close();
       }
       e.Result = tag[1];
   }

   private void backgroundWorker2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
   {
       this.progressBar1.Visible = false;
       int size = Convert.ToInt32(e.Result.ToString());
       string fs = "";
       if (size < KB)
       {
           fs =  string.Format("{0} bytes", size);
       }
       else if (size < MB)
       {
           fs = string.Format("{0} KB", (size / KB));
       }
       else if (size < GB)
       {
           fs = string.Format("{0:0.00} MB", (size / MB));
       }
       else
       {
           fs = string.Format("{0:0.00} GB", (size / GB));
       }
       MessageBox.Show(fs + " Downloaded");
       rundlQue();
   }

   private void backgroundWorker2_ProgressChanged(object sender, ProgressChangedEventArgs e)
   {
       this.progressBar1.Visible = true;
       this.progressBar1.Value = e.ProgressPercentage;
   }
   
        #endregion

   #region get/set permission and copy/cust file;

   private void setPermissionToolStripMenuItem_Click(object sender, EventArgs e)
       {
         if (treeView1.SelectedNode != null)
       
                   {

                       string filename = treeView1.SelectedNode.FullPath.Replace("\\", "/").Replace("//", "/");
                       Form4 setp = new Form4();
                       setp.cs = activeSession;
                       setp.pth = filename;
                       setp.Show();
       
                   }
       
                   else { MessageBox.Show("Please select."); }


            }


        private void getPermissionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode != null && activeSession.IsConnected)
            {
                string cmd;
                if (treeView1.SelectedNode.ImageIndex==1) cmd = "ls -l " + treeView1.SelectedNode.FullPath.Replace("\\", "/").Replace("//", "/");
                else cmd = "ls -ld " + treeView1.SelectedNode.FullPath.Replace("\\", "/").Replace("//", "/");

                string[] statment = new string[] {"filename x pipe " + "\"" + cmd + "\";","data _null_;", "infile x dsd truncover;", "input text $100.;", "file '/tmp/pf';", "put text;", "run;"};
                activeSession.Workspace.LanguageService.SubmitLines(statment);

                Array todld;
                SAS.BinaryStream stm = null;
                string pf;
                bool endOfFile = false;
                string dldfileref = "";

                MemoryStream stream = new MemoryStream();

                try
                {
                    stm = activeSession.Workspace.FileService.AssignFileref("", "DISK", "/tmp/pf", "", out dldfileref).OpenBinaryStream(SAS.StreamOpenMode.StreamOpenModeForReading);

                    do
                    {

                        stm.Read(MB, out todld);
                        stream.Write((byte[])todld, 0, todld.Length);
                        endOfFile = (todld.Length < MB);
                    } while (!endOfFile);

                    if (stm != null) stm.Close();
                }
                catch { }
                finally
                {
                    stream.Position = 0;
                    StreamReader reader = new StreamReader(stream);
                    pf = reader.ReadToEnd();
                    if (stream != null) stream.Close();
                    if (reader != null) reader.Close();
                }
                MessageBox.Show(showp(pf));


            }
            else { MessageBox.Show("Please select."); }
        }


        private string showp(string pf)
        {
            string o = pf.Substring(1,3);
            string g = pf.Substring(4,3);
            string u = pf.Substring(7,3);
            string showp = "Owner: "+o+"\n"+"Group: "+g+"\n"+"Other: "+u;
            return showp;               
        }


        private string mvdir = "";
        private bool iscut = false;
        
        Queue<string> _queuedcm = new Queue<string>();

        //private void SubmitComplete(int rc)
        //{
        //    MessageBox.Show("Completed");

        //}

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BackgroundWorker bg = new BackgroundWorker();
            bg.DoWork += bg_DoWork;
            bg.RunWorkerCompleted += bg_RunWorkerCompleted;

            //activeSession.Workspace.LanguageService.SubmitComplete += new SAS.CILanguageEvents_SubmitCompleteEventHandler(SubmitComplete);


            if (activeSession != null && activeSession.IsConnected && treeView1.SelectedNode != null && treeView1.SelectedNode.ImageIndex == 0 && mvdir != string.Empty)
            {
                string path = treeView1.SelectedNode.FullPath.Replace("\\", "/").Replace("//", "/");
                if (iscut)
                {
                    string statement = "x mv " + mvdir + " " + path + ";";

                    _queuedcm.Enqueue(statement);

                }
                else
                {
                    string statement = "x cp " + mvdir + " " + path + ";";

                    _queuedcm.Enqueue(statement);

                }

                runcmQue(bg);

                treeView1.SelectedNode.Collapse();
                string[] tag = (string[])treeView1.SelectedNode.Tag;
                treeView1.SelectedNode.Tag = new string[] { tag[0], tag[1], "A" };
            }
            else { MessageBox.Show("Nothing to paste."); }
        }


        private void runcmQue(BackgroundWorker bg)
        {
            if (_queuedcm.Count != 0)
            {
                string curr = _queuedcm.Dequeue();

                if (!bg.IsBusy && curr != null)
                {
                    bg.RunWorkerAsync(curr); 
                }
            }
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {


            if (treeView1.SelectedNode != null && treeView1.SelectedNode.ImageIndex == 1)
            {
                mvdir = treeView1.SelectedNode.FullPath.Replace("\\", "/").Replace("//", "/");
                iscut = false;
            }
            else { MessageBox.Show("Select the file."); }

        }


        void bg_DoWork(object sender, DoWorkEventArgs e)
        {
            //MessageBox.Show(e.Argument.ToString());
            activeSession.Workspace.LanguageService.Submit(e.Argument.ToString());
        }

        void bg_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //File.WriteAllText(@"d:\data\541395034\Desktop\Work\tl.txt", activeSession.Workspace.LanguageService.FlushLog(100000));
            runcmQue((BackgroundWorker)sender);
        }


        private void moveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode != null && treeView1.SelectedNode.ImageIndex == 1)
            {
                mvdir = treeView1.SelectedNode.FullPath.Replace("\\", "/").Replace("//", "/");
                iscut = true;
                string[] tag = (string[])treeView1.SelectedNode.Parent.Tag;
                treeView1.SelectedNode.Parent.Tag = new string[] {tag[0],tag[1],"A"};
            }
            else { MessageBox.Show("Select the file."); }
        }
   #endregion

        private void transferModeToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void binaryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.binaryToolStripMenuItem.Checked = true;
            this.textToolStripMenuItem.Checked = false;
        }

        private void textToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.binaryToolStripMenuItem.Checked = false;
            this.textToolStripMenuItem.Checked = true;
        }

    }
}







