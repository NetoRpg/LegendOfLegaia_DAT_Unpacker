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
                int fileNum = fs.extractPiece(0, 4, 4).extractInt32() + 1;
                int headerSize = fs.extractPiece(0, 4).extractInt32() * 0x800;
                int[] offsets = new int[fileNum];
                string fileName = Path.GetFileNameWithoutExtension(_FILEPATH);
                byte[] data;

                // Carregando o header na memória, ganhando velocidade na leitura dos valores.
                // Menos acessos ao disco, o que é relativamente demorado.
                MemoryStream header = new MemoryStream(fs.extractPiece(0, headerSize, 0));
                header.Position = 8;

                // Fazer a leitura de todos os offsets
                for (int i = 0; i < fileNum; i++)
                {
                    offsets[i] = header.extractPiece(0, 4).extractInt32() * 0x800;
                }

                string basePath = Path.Combine(Path.GetDirectoryName(_FILEPATH), fileName);
                Directory.CreateDirectory(basePath);

                // Extrair e salvar os arquivos.
                for (int i = 0; i < fileNum - 1; i++)
                {
                    data = fs.extractPiece(0, offsets[i + 1] - offsets[i]);
                    if (data[3] == 0x01 && data[2] < 0x10)
                    {

                        int timNum = data.extractInt32(4);
                        int[] timOffsets = new int[timNum + 1];

                        for (int x = 0; x < timNum; x++)
                        {
                            timOffsets[x] = (data.extractInt32((4 * x) + 8) * 4) + 4;
                        }

                        timOffsets[timOffsets.Length - 1] = data.Length;

                        for (int x = 0; x < timOffsets.Length - 1; x++)
                        {
                            string timPath = Path.Combine(basePath, String.Format("{0}_{1}", fileName, i));
                            Directory.CreateDirectory(timPath);

                            string ext = (data[timOffsets[x]] == 0x10) ? "TIM" : "BIN";
                            data.Save(Path.Combine(timPath, String.Format("{0}_{1}_{2}.{3}", fileName, i, x, ext)), timOffsets[x], timOffsets[x + 1] - timOffsets[x]);
                        }

                        //continue;
                    }

                    data.Save(Path.Combine(basePath, String.Format("{0}_{1}.BIN", fileName, i)));               
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
