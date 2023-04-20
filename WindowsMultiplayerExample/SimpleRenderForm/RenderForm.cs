using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Windows.Forms;
using GameShared;

namespace SimpleRenderForm
{
    public partial class RenderForm : Form
    {
        private int tileSize = 16;
        private float unit = 0;
        private float factor = 1080.0f / 1920.0f;
        private float frametime;
        private int heightPadding;


        private readonly Image image;
        private readonly Rectangle[] animationFrames = new Rectangle[]{
            new Rectangle(43, 9, 16, 16),
            new Rectangle(60, 9, 16, 16),
            new Rectangle(77, 9, 16, 16)
            };

        private string mainUrl = "http://localhost:5127/game";
        private HttpClient client = new HttpClient();

        private RenderObject myPlayer;
        private readonly List<RenderObject> renderObjects = new();

        private Font nameFont = new Font(FontFamily.GenericMonospace, 8);

        public RenderForm()
        {
            InitializeComponent();
            image = Bitmap.FromFile("NES - Super Mario Bros - Mario & Luigi.png");

            DoubleBuffered = true;
            ResizeRedraw = true;

            KeyDown += RenderForm_KeyDown;
            FormClosing += Form1_FormClosing;
            SizeChanged += RenderForm_SizeChanged;
            Resize += RenderForm_Resize;
            Load += RenderForm_Load;
        }

        private void RenderForm_Load(object sender, EventArgs e)
        {
            heightPadding = ClientSize.Height - Height;
            RenderForm_SizeChanged(null, null);

            int[] ingame = ToInGame(Bounds.Width, Bounds.Height);
            Random r = new();
            myPlayer = new()
            {
                rectangle = new Rectangle(r.Next(0, ingame[0]), r.Next(0, ingame[1]), tileSize, tileSize),
                dir = new Point(0, 0),
                name = Guid.NewGuid().ToString().Substring(0, 8)
            };
            renderObjects.Add(myPlayer);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            image.Dispose();
        }

        private void RenderForm_Resize(object sender, EventArgs e)
        {
            RenderForm_SizeChanged(null, null);
        }

        private void RenderForm_SizeChanged(object sender, EventArgs e)
        {
            unit = ClientSize.Width / (float)(20 * tileSize);
            int height = (int)(ClientSize.Width * factor);
            if (ClientSize.Height != 0)
            {
                Debug.WriteLine(">" + Height);
                Height = heightPadding + height;
                Debug.WriteLine(Height);
            }
        }

        private int[] ToInGame(int x, int y)
        {
            return new int[]
            {
                (int)(x/ unit)
                ,(int)(y/ unit)
            };
        }

        private void RenderForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.A)
            {
                myPlayer.dir.X = -1;
            }
            //kijk eens of je de andere richtingen ook kan aanpassen.
            //kijk eens of je ook de player kan laten stoppen, hint: je hebt een keyup event nodig!
        }


        public void Logic(float frametime)
        {
            this.frametime = frametime;
            MoveSprite(myPlayer, frametime);

            SendMyState();

            GetOtherPlayers();
        }

        private void GetOtherPlayers()
        {
            List<Player> players = GetMessages(client);
            foreach (Player player in players)
            {
                if (player.id != myPlayer.name)
                {
                    RenderObject other = renderObjects.FirstOrDefault(i => i.name == player.id);
                    if (other == null)
                    {
                        other = AddNewlyEnteredPlayer(player);
                    }
                    other.rectangle.X = player.x;
                    other.rectangle.Y = player.y;
                }
            }
        }

        private RenderObject AddNewlyEnteredPlayer(Player player)
        {
            RenderObject other = new()
            {
                rectangle = new Rectangle(player.x, player.y, tileSize, tileSize),
                dir = new Point(0, 0),
                name = player.id
            };
            renderObjects.Add(other);
            return other;
        }

        private void SendMyState()
        {
            Player message = new Player()
            {
                id = myPlayer.name,
                x = (int)myPlayer.rectangle.X,
                y = (int)myPlayer.rectangle.Y
            };
            PutMessages(client, message);
        }

        private void PutMessages(HttpClient client, Player message)
        {
            string jsonString = JsonSerializer.Serialize(message);

            string requestUri = mainUrl;
            Console.WriteLine(requestUri);
            using (StringContent jsonContent = new(jsonString, Encoding.UTF8, "application/json"))
            using (HttpResponseMessage resp = client.PostAsync(requestUri, jsonContent).Result)
            {
                if (resp != null)
                {
                    string messages = resp.Content.ReadAsStringAsync().Result;
                    Console.WriteLine(messages);
                }
            }
        }

        private List<Player> GetMessages(HttpClient client)
        {
            string requestUri = mainUrl;
            Console.WriteLine(requestUri);
            using (HttpResponseMessage resp = client.GetAsync(requestUri).Result)
            {
                if (resp != null)
                {
                    string messages = resp.Content.ReadAsStringAsync().Result;
                    Console.WriteLine(messages);
                    if (string.IsNullOrEmpty(messages.Trim()))
                    {
                        return null;
                    }
                    List<Player> msg = JsonSerializer.Deserialize<List<Player>>(messages);


                    return msg;
                }
            }
            return null;
        }

        private void MoveSprite(RenderObject item, float frametime)
        {
            item.rectangle.X += item.dir.X * item.speed * frametime;
            item.rectangle.Y += item.dir.Y * item.speed * frametime;
            //Debug.WriteLine(frametime + " " + (item.dir.X * item.speed * frametime));
            if (item.rectangle.X <= 0)
            {
                item.dir.X = 1;
            }
            if (item.rectangle.Y <= 0)
            {
                item.dir.Y = 1;
            }

            int[] ingame = ToInGame(Bounds.Width, Bounds.Height);
            if (item.rectangle.Right >= ingame[0])
            {
                item.dir.X = -1;
            }
            if (item.rectangle.Bottom >= ingame[1])
            {
                item.dir.Y = -1;
            }
        }
        private void MoveFrame(RenderObject item)
        {
            item.frame += frametime * item.animationSpeed;
            if (item.frame >= animationFrames.Length)
            {
                item.frame = 0;
            }
        }

        private Graphics InitGraphics(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.None;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.Transform = new Matrix();
            g.ScaleTransform(unit, unit);

            g.Clear(Color.Black);
            return g;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = InitGraphics(e);
            foreach (RenderObject item in renderObjects)
            {
                g.DrawImage(image, item.rectangle, animationFrames[(int)item.frame], GraphicsUnit.Pixel);
                g.DrawString(item.name, nameFont, Brushes.White, item.rectangle.X, item.rectangle.Y - 20);
                MoveFrame(item);
            }

        }
    }
}
