using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Gomoku
{
    /// <summary>
    /// GameWindow.xaml 的交互逻辑
    /// </summary>
    public partial class GameWindow : Window
    {
        private Button[][] _buttons;
        private SquareStatus[][] _squares;
        // 位置反查表
        private Dictionary<Button, Tuple<int, int>> _squareMap;
        private Button _lastBlackOne, _lastWhiteOne;
        private StringBuilder _logBlockBuilder;
        private bool _isEnd = false;
        private bool _isBlack = false;
        private bool _isOurTurn = false;
        private bool _ignoreThis = false;

        private HubConnection gameHub;

        public GameWindow(string svrAddress)
        {
            InitializeComponent();
            _buttons = new Button[15][];
            _squares = new SquareStatus[15][];
            _squareMap = new Dictionary<Button, Tuple<int, int>>();
            for (int i = 0; i < 15; i++)
            {
                _buttons[i] = new Button[15];
                _squares[i] = new SquareStatus[15];
                for (int j = 0; j < 15; j++)
                {
                    _buttons[i][j] = new Button()
                    {
                        Template = (ControlTemplate)Resources["EmptySquare"],
                    };
                    _buttons[i][j].Click += OnGamePieceClicked;
                    GameBoard.Children.Add(_buttons[i][j]);
                    Grid.SetRow(_buttons[i][j], i);
                    Grid.SetColumn(_buttons[i][j], j);
                    _squares[i][j] = SquareStatus.Empty;
                    _squareMap.Add(_buttons[i][j], Tuple.Create(i, j));
                }
            }
            _logBlockBuilder = new StringBuilder();
            gameHub = new HubConnectionBuilder()
                .WithUrl(svrAddress)
                .Build();
            gameHub.On<bool>("GameStart", isot =>
            {
                Dispatcher.Invoke(() =>
                {
                    _isOurTurn = isot;
                    _isBlack = isot;
                    LoadingBorder.Visibility = Visibility.Collapsed;
                    StatusDisplay.Text = $"{(_isBlack ? '我' : '对')}方先手，{(_isOurTurn ? '我' : '对')}方回合";
                    if (!_isOurTurn)
                        LockAllButton();
                    else
                        UnlockAllUnusedButton();
                    UpdateGameStatus();
                });
            });
            gameHub.On("GameEndForce", () =>
            {
                Dispatcher.Invoke(() =>
                {
                    LockAllButton();
                    _logBlockBuilder.AppendLine("游戏因其他原因结束");
                    UpdateGameStatus();
                });
            });
            gameHub.On<int, int>("DownGamePiece", (y, x) =>
            {
                if (!_ignoreThis)
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (!_isBlack)
                            DownBlackGamePiece(x, y);
                        else
                            DownWhiteGamePiece(x, y);
                    });
                }
                _ignoreThis = false;
            });
            Start();
        }

        public async void Start()
        {
            await gameHub.StartAsync();
            await gameHub.SendAsync("EnterQueue");
        }

        private void LockAllButton()
        {
            foreach (var i in _buttons)
                foreach (var j in i)
                    j.IsEnabled = false;
        }

        private void UnlockAllUnusedButton()
        {
            for (int i = 0; i < 15; i++)
                for (int j = 0; j < 15; j++)
                    if (_squares[i][j] == SquareStatus.Empty)
                        _buttons[i][j].IsEnabled = true;
        }

        public async void OnGamePieceClicked(object sender, RoutedEventArgs e)
        {
            var realSender = (Button)sender;
            var addr = _squareMap[realSender];
            if (_isBlack)
                DownBlackGamePiece(addr.Item2, addr.Item1);
            else
                DownWhiteGamePiece(addr.Item2, addr.Item1);
            _ignoreThis = true;
            await gameHub.SendAsync("DownGamePiece", addr.Item1, addr.Item2);
        }

        public void DownBlackGamePiece(int x, int y)
        {
            if (_lastBlackOne != null)
                _lastBlackOne.Template = (ControlTemplate)Resources["BlackGamePiece"];
            _lastBlackOne = _buttons[y][x];
            _squares[y][x] = SquareStatus.UsedByBlack;
            _buttons[y][x].IsEnabled = false;
            _buttons[y][x].Template = (ControlTemplate)Resources["BlackNewGamePiece"];
            _logBlockBuilder.AppendLine($"黑方：在{Cord2Loc(y, x)}落子");
            _isOurTurn = !_isOurTurn;
            StatusDisplay.Text = $"{(_isBlack ? '我' : '对')}方先手，{(_isOurTurn ? '我' : '对')}方回合";
            if (!_isOurTurn)
                LockAllButton();
            else
                UnlockAllUnusedButton();
            UpdateGameStatus();
            if (CheckForWin(out var isBlack, out var winAddress))
            {
                _logBlockBuilder.AppendLine($"{(isBlack ? '黑' : '白')}方胜利！位置：{winAddress}");
                UpdateGameStatus();
                _isEnd = true;
                LockAllButton();
            }
        }

        public void DownWhiteGamePiece(int x, int y)
        {
            if (_lastWhiteOne != null)
                _lastWhiteOne.Template = (ControlTemplate)Resources["WhiteGamePiece"];
            _lastWhiteOne = _buttons[y][x];
            _squares[y][x] = SquareStatus.UsedByWhite;
            _buttons[y][x].IsEnabled = false;
            _buttons[y][x].Template = (ControlTemplate)Resources["WhiteNewGamePiece"];
            _logBlockBuilder.AppendLine($"白方：在{Cord2Loc(y, x)}落子");
            _isOurTurn = !_isOurTurn;
            StatusDisplay.Text = $"{(_isBlack ? '我' : '对')}方先手，{(_isOurTurn ? '我' : '对')}方回合";
            if (!_isOurTurn)
                LockAllButton();
            else
                UnlockAllUnusedButton();
            UpdateGameStatus();
            if (CheckForWin(out var isBlack, out var winAddress))
            {
                _logBlockBuilder.AppendLine($"{(isBlack ? '黑' : '白')}方胜利！位置：{winAddress}");
                UpdateGameStatus();
                _isEnd = true;
                LockAllButton();
            }
        }

        protected override async void OnClosed(EventArgs e)
        {
            await gameHub.DisposeAsync();
            base.OnClosed(e);
        }

        private string Cord2Loc(int row, int col) =>
            $"{(char)(col + 'A')}{15 - row}";

        private void UpdateGameStatus()
        {
            LogBlock.Text = _logBlockBuilder.ToString();
            LogScrollViewer.ScrollToBottom();
        }

        private bool CheckForWin(out bool isBlack, out string winAddress)
        {
            for (int i = 0; i < 15; i++)
            {
                int lastContinuous = 1;
                // 横着的
                for (int j = 1; j < 15; j++)
                {
                    if (_squares[i][j] != SquareStatus.Empty && _squares[i][j] == _squares[i][j - 1])
                    {
                        lastContinuous++;
                        if (lastContinuous >= 5)
                        {
                            isBlack = _squares[i][j] == SquareStatus.UsedByBlack;
                            var sb = new StringBuilder();
                            sb.Append(Cord2Loc(i, j + 1 - lastContinuous));
                            for (int k = lastContinuous - 2; k >= 0; k--)
                                sb.Append($"，{Cord2Loc(i, j - k)}");
                            winAddress = sb.ToString();
                            return true;
                        }
                    }
                    else
                    {
                        lastContinuous = 1;
                    }
                }
                lastContinuous = 1;
                // 竖着的
                for (int j = 1; j < 15; j++)
                {
                    if (_squares[j][i] != SquareStatus.Empty && _squares[j][i] == _squares[j - 1][i])
                    {
                        lastContinuous++;
                        if (lastContinuous >= 5)
                        {
                            isBlack = _squares[j][i] == SquareStatus.UsedByBlack;
                            var sb = new StringBuilder();
                            sb.Append(Cord2Loc(j + 1 - lastContinuous, i));
                            for (int k = lastContinuous - 2; k >= 0; k--)
                                sb.Append($"，{Cord2Loc(j - k, i)}");
                            winAddress = sb.ToString();
                            return true;
                        }
                    }
                    else
                    {
                        lastContinuous = 1;
                    }
                }
            }
            // 斜着的
            int startX = 4, startY = 0;
            // 右上到左下
            for (; startX < 15; startX++)
            {
                int lastContinuous = 1;
                for (int x = startX - 1, y = startY + 1; x >= 0 && y < 15; x--, y++)
                {
                    if (_squares[y][x] != SquareStatus.Empty && _squares[y][x] == _squares[y - 1][x + 1])
                    {
                        lastContinuous++;
                        if (lastContinuous >= 5)
                        {
                            isBlack = _squares[y][x] == SquareStatus.UsedByBlack;
                            var sb = new StringBuilder();
                            sb.Append(Cord2Loc(y--, x++));
                            lastContinuous--;
                            for (; lastContinuous > 0; lastContinuous--, y--, x++)
                                sb.Append($"，{Cord2Loc(y, x)}");
                            winAddress = sb.ToString();
                            return true;
                        }
                    }
                    else
                        lastContinuous = 1;
                }
            }
            startX = 14;
            for (; startY < 11; startY++)
            {
                int lastContinuous = 1;
                for (int x = startX - 1, y = startY + 1; x >= 0 && y < 15; x--, y++)
                {
                    if (_squares[y][x] != SquareStatus.Empty && _squares[y][x] == _squares[y - 1][x + 1])
                    {
                        lastContinuous++;
                        if (lastContinuous >= 5)
                        {
                            isBlack = _squares[y][x] == SquareStatus.UsedByBlack;
                            var sb = new StringBuilder();
                            sb.Append(Cord2Loc(y--, x++));
                            lastContinuous--;
                            for (; lastContinuous > 0; lastContinuous--, y--, x++)
                                sb.Append($"，{Cord2Loc(y, x)}");
                            winAddress = sb.ToString();
                            return true;
                        }
                    }
                    else
                        lastContinuous = 1;
                }
            }
            // 左上到右下
            startX = 10;
            startY = 0;
            for(; startX >= 0; startX--)
            {
                int lastContinuous = 1;
                for (int x = startX + 1, y = startY + 1; x < 15 && y < 15; x++, y++)
                {
                    if (_squares[y][x] != SquareStatus.Empty && _squares[y][x] == _squares[y - 1][x - 1])
                    {
                        lastContinuous++;
                        if (lastContinuous >= 5)
                        {
                            isBlack = _squares[y][x] == SquareStatus.UsedByBlack;
                            var sb = new StringBuilder();
                            sb.Append(Cord2Loc(y--, x--));
                            lastContinuous--;
                            for (; lastContinuous > 0; lastContinuous--, y--, x--)
                                sb.Append($"，{Cord2Loc(y, x)}");
                            winAddress = sb.ToString();
                            return true;
                        }
                    }
                    else
                        lastContinuous = 1;
                }
            }
            startX = 0;
            startY = 1;
            for (; startY < 15; startY++)
            {
                int lastContinuous = 1;
                for (int x = startX + 1, y = startY + 1; x < 15 && y < 15; x++, y++)
                {
                    if (_squares[y][x] != SquareStatus.Empty && _squares[y][x] == _squares[y - 1][x - 1])
                    {
                        lastContinuous++;
                        if (lastContinuous >= 5)
                        {
                            isBlack = _squares[y][x] == SquareStatus.UsedByBlack;
                            var sb = new StringBuilder();
                            sb.Append(Cord2Loc(y--, x--));
                            lastContinuous--;
                            for (; lastContinuous > 0; lastContinuous--, y--, x--)
                                sb.Append($"，{Cord2Loc(y, x)}");
                            winAddress = sb.ToString();
                            return true;
                        }
                    }
                    else
                        lastContinuous = 1;
                }
            }
            isBlack = false;
            winAddress = "";
            return false;
        }
    }
}
