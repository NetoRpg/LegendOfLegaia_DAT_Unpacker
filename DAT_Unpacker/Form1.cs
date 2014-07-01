using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace DAT_Unpacker
{
    public partial class DUP : Form
    {
        public DUP()
        {
            InitializeComponent();
        }

        string _FILEPATH;

        private void btnUnpack_Click(object sender, EventArgs e)
        {
            if (_FILEPATH == null) return;

            using (FileStream fs = new FileStream(_FILEPATH, FileMode.Open))
            { 
                fs.Position = 4;

                int fileNum = fs.extractPiece(0, 4).extractInt32() + 1;
                int headerSize = fs.extractPiece(0, 4).extractInt32() * 0x800;
                int[] offsets = new int[fileNum];

                fs.Position = 0;

                // Carregando o header na memória, ganhando velocidade na leitura dos valores.
                // Menos acessos ao disco, o que é relativamente demorado.
                MemoryStream header = new MemoryStream(fs.extractPiece(0, headerSize));
                header.Position = 8;

                // Fazer a leitura de todos os offsets
                for (int i = 0; i < fileNum; i++)
                {
                    offsets[i] = header.extractPiece(0, 4).extractInt32() * 0x800;
                }

                string basePath = Path.Combine(Path.GetDirectoryName(_FILEPATH), Path.GetFileNameWithoutExtension(_FILEPATH));
                Directory.CreateDirectory(basePath);

                // Extrair e salvar os arquivos.
                for (int i = 0; i < fileNum - 1; i++)
                {
                    fs.extractPiece(0, offsets[i + 1] - offsets[i]).Save(Path.Combine(basePath, i.ToString() + ".BIN"));               
                }

                MessageBox.Show("Successfully Unpacked!");
            }
        }

        private void btnSelect_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "DAT Files|*.DAT";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtPath.Text = _FILEPATH = ofd.FileName;
                }
            }
        }
    }
}
