using System;
using System.IO;
using System.Windows.Forms;

namespace TTG_Tools
{
    public partial class PropEditor : Form
    {
        public PropEditor()
        {
            InitializeComponent();
        }

        private void openPropBtn_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Property files (*.prop)|*.prop|All files (*.*)|*.*";
                ofd.FilterIndex = 0;

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    LoadProp(ofd.FileName);
                }
            }
        }

        private void LoadProp(string filePath)
        {
            try
            {
                propPathTB.Text = filePath;
                propTreeView.BeginUpdate();
                propTreeView.Nodes.Clear();

                PropInspectorParser.PropNode root = PropInspectorParser.Parse(filePath);
                TreeNode rootNode = CreateTreeNode(root);
                propTreeView.Nodes.Add(rootNode);
                rootNode.Expand();

                statusLbl.Text = string.Format("Loaded {0} ({1:N0} bytes)", Path.GetFileName(filePath), new FileInfo(filePath).Length);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not parse .prop file.\n\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLbl.Text = "Failed to parse file.";
            }
            finally
            {
                propTreeView.EndUpdate();
            }
        }

        private TreeNode CreateTreeNode(PropInspectorParser.PropNode node)
        {
            string text = string.IsNullOrEmpty(node.Value) ? node.Name : string.Format("{0}: {1}", node.Name, node.Value);
            TreeNode treeNode = new TreeNode(text);
            treeNode.Tag = node;

            foreach (PropInspectorParser.PropNode child in node.Children)
            {
                treeNode.Nodes.Add(CreateTreeNode(child));
            }

            return treeNode;
        }

        private void expandAllBtn_Click(object sender, EventArgs e)
        {
            propTreeView.ExpandAll();
        }

        private void collapseAllBtn_Click(object sender, EventArgs e)
        {
            propTreeView.CollapseAll();
        }
    }
}
