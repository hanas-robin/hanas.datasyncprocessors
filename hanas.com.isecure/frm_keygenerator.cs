using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace hanas.com.isecure
{
    public partial class frm_keygenerator : Form
    {
        private string c_encryptedkey = string.Empty;
        private string c_decryptedkey = string.Empty;
        private string c_removedquote = string.Empty;

        isecure.cls_isecure00 c_isSecure = new isecure.cls_isecure00();

        public frm_keygenerator()
        {
            InitializeComponent();
        }

        private void btnEncrypt_Click(object sender, EventArgs e)
        {
            c_encryptedkey = c_isSecure.tEncrypt(txtData.Text);

            txtEncryptedKey.Text = c_encryptedkey;
        }

        private void btnDecrypt_Click(object sender, EventArgs e)
        {
            c_decryptedkey = c_isSecure.tDecrypt(txtData.Text);

            txtDecryptedKey.Text = c_decryptedkey;
        }

        private void btnRemoveDQuote_Click(object sender, EventArgs e)
        {
            c_removedquote = txtData.Text.Replace("''", "'");

            txtRemovedQuote.Text = c_removedquote;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
