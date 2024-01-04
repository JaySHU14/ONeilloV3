using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO; // 
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json; // install reference to Newtonsoft through NuGet packages
using System.Web;

namespace ONeilloGameV3
{
    public partial class Form1 : Form
    {
        private const int boardSize = 8; // generate a standard for the size of the board. This allows us to iterate over the actual size of the board and set a boundary for cells stored to it
        private const int cellSize = 50;
        private int[,] board = new int[boardSize, boardSize];
        private int currentPlayer = 1;
        private int blackCount = 2;
        private int whiteCount = 2;
        private Timer updateTimer;

        private const int maxSaves = 5; // 5 maximum game states allowed
        private const string savePath = "game_data.json"; // setting name for the file in which the game states will be stored to
        private List<GameData> gameStates = new List<GameData>(); // new list representing the data stored

        public Form1()
        {
            InitializeComponent();
            FormComponents();
            InitialiseBoard();
            SetBoard();
            LoadGame();
            infoPanel.Visible = true;
            informationPanelToolStripMenuItem.Checked = true;
            updateTimer = new Timer(); // create a new instance of the Timer class, assign it to the new var updateTimer
            updateTimer.Interval = 1000 / 60; // 60 FPS refresh rate. Used to control how often the tick event will be triggered (1000 / 60 milliseconds) therefore the UpdateBoard method will be called 60 times per second 
            updateTimer.Tick += new EventHandler((s, e) => { UpdateBoard(); }); // attach an event handler to the Tick event. This is defined using a lambda expression, meaning when the timer ticks, the UpdateBoard method will be called. 
            updateTimer.Start(); // start the timer when the form is initialised
        }

        private void FormComponents() // set the characteristics of the form
        {
            int width = (boardSize * cellSize) + 20; // declare the form's width, allow for padding
            int height = boardSize * cellSize + cellSize * 4; // declare the form's height, allow for info panel to be displayed at the bottom. transferred to design specs

            this.Text = "ONeillo V3"; // set the name of the form at the top of the application
            this.BackColor = Color.Green; // set the background of the form to green (matches cell colours)
            this.Size = new Size(width, height); // create a new size for the form, initialising the width and height variables declared previously

            infoPanel.Visible = false; // at first, we do not want the infoPanel to be visible

            if (informationPanelToolStripMenuItem.Checked)
            {
                infoPanel.Visible = true;
            }
        }

        private void InitialiseBoard()
        {
            board[3, 4] = board[4, 3] = 1; // set starting black counters
            board[3, 3] = board[4, 4] = 2; // set starting white counters
        }

        private void SetBoard() // this function will create and display the buttons in a list format to represent the board
        {
            List<Button> buttons = new List<Button>(); // create a new list for the buttons to be stored to

            for (int row = 0; row < boardSize; row++) // iterate over each row in the size of the board
            {
                for (int col = 0; col < boardSize; col++) // iterate over each column in the size of the board
                {
                    Button button = new Button(); // for every individual position on the board, create a new button
                    button.Size = new Size(cellSize, cellSize); // set the button size to the size of the cells (50x50)
                    button.Location = new Point(col * cellSize, row * cellSize + 30); // set the locations of the buttons, giving each a unique location to be found by
                    button.Click += new EventHandler(CellClicked); // creating an event handler to handle the event of a click. when the user clicks a cell, whatever is found in the CellClicked function will run
                    button.Name = "btn_" + row + "_" + col; // set the name of the button to btn_2_4 etc.

                    if (board[row, col] == 1) // if the current cell has a value of 1
                    {
                        button.BackColor = Color.Black; // set a black counter to it
                    }
                    else if (board[row, col] == 2) // if the current cell has a value of 2
                    {
                        button.BackColor = Color.White; // set a white counter to it
                    }
                    else
                    {
                        if (ValidMove(row, col))
                        {
                            button.BackColor = Color.LightGreen; // change cell colour for a valid move
                        }

                        else // it is an empty cell
                        {
                            button.BackColor = Color.Green; // set it to empty (green)
                        }
                    }

                    buttons.Add(button); // add button control to form
                }
            }

            Controls.AddRange(buttons.ToArray()); // convert every button stored to the 'buttons' list to an array to be iterated over
        }

        private void CellClicked(object sender, EventArgs e) // event handler for when a cell is clicked
        {
            Button button = (Button)sender; // create an instance of Button to represent the input sent by the user
            int row = button.Location.Y / cellSize; // determine a variable to represent the specific row in play
            int col = button.Location.X / cellSize; // determine a variable to represent the specific col in play

            if (ValidMove(row, col)) // if the move is valid on the newly initialised row and col variables...
            {
                MakeMove(row, col); // make the move

                UpdateBoard(); // update the board on the UI

                if (GameOver())
                {
                    GameOverMessage(); // if the game is over, display the message box that declares a game over
                }
                else
                {
                    SwitchPlayer(); // if the game is not over, switch the player 
                }
            }
        }

        private void MakeMove(int row, int col) // declare function to make a move on the board
        {
            int[] directionRow = { -1, -1, -1, 0, 1, 1, 1, 0 }; // array to represent the co-ordinates of each cell to be checked upon (-1,-1 top left, -1,0 left etc.)
            int[] directionCol = { -1, 0, 1, 1, 1, 0, -1, -1 };

            int newCount = 0;

            board[row, col] = currentPlayer; // current cell position is equal to the player that has their turn



            for (int i = 0; i < 8; i++) // iterate over the 8 possible directions a piece can be captured through
            {
                int r = row + directionRow[i]; // temporary variables used to show the next cell in the current direction 
                int c = col + directionCol[i];

                if (r < 0 || r >= boardSize || c < 0 || c >= boardSize || board[r, c] != OtherPlayer()) // if the initial conditions for capturing an opposing piece are met then continue, else skip to the next iteration of the loop
                {
                    continue;
                }

                while (true)
                {
                    r += directionRow[i]; // increment the temp variables to move to the next desired cell in the current direction (determined by directionRow)
                    c += directionCol[i];

                    if (r < 0 || r >= boardSize || c < 0 || c >= boardSize) // checks whether the new positions of r and c are within the boundaries of the game board
                    {
                        break; // if outside the boundaries, the loop breaks to indicate that the end of the board in the current direction has been reached
                    }

                    if (board[r, c] == 0) // if the cell at the current position contains 0, the loop will break, as it means that the sequence of opponent pieces in the current direction has been halted by an empty cell
                    {
                        break;
                    }

                    if (board[r, c] == currentPlayer) // if the cell contains the current player's piece, it means that a sequence of opponent pieces has been found
                    {
                        int count = Math.Abs(r - row) + Math.Abs(c - col); // calculates the count of opponent pieces in the sequence found between original + new positions
                        for (int j = 1; j < Math.Min(count, Math.Min(Math.Min(boardSize - row, row + 1), Math.Min(boardSize - col, col + 1))); j++) // ensuring that the count variable doesnt exceed the value of valid cells in the certain direction being looked at
                        {
                            r -= directionRow[i]; // loop responsible for flipping opponent pieces
                            c -= directionCol[i]; // in order to update the cells we will need to move in the opposite direction from the current position back to the original.
                            board[r, c] = currentPlayer; // the specific cell is set to the current player's value, flipping the opponent's piece to the current player's (capturing it)
                            newCount++; // keeps track of pieces flipped
                        }
                        break; // after flipping all opponent's pieces in the specific direction, break the loop
                    }
                }
            }

            if (currentPlayer == 1) // if current player is black...
            {
                blackCount += newCount + 1; // add current black count to the new count of updating
                whiteCount -= newCount; // remove any necessary lost white counters to the new count of updating
            }
            else // else white
            {
                whiteCount += newCount + 1; // increment white count
                blackCount -= newCount; // decrement black count
            }
        }

        private bool ValidMove(int row, int col)
        {
            if (board[row, col] != 0) // check if cell is already occupied on the board
            {
                return false;
            }

            int[] directionRow = { -1, -1, -1, 0, 1, 1, 1, 0 }; // determining every direction to be made
            int[] directionCol = { -1, 0, 1, 1, 1, 0, -1, -1 };
            bool isValidMove = false; // default a valid move to false as we only want a move to be valid once it has met the specific conditions

            for (int i = 0; i < 8; i++) // for loop determining the boundaries in the 8 different directions
            {
                int r = row + directionRow[i]; // new temp variable to represent the new change in the rows and cols
                int c = col + directionCol[i];

                if (r < 0 || r >= boardSize || c < 0 || c >= boardSize || board[r, c] != OtherPlayer()) // ensuring that the moves to be made are within the boundaries of the board
                {
                    continue;
                }

                while (true)
                {
                    r += directionRow[i]; // new temp vars representing next move
                    c += directionCol[i];

                    if (r < 0 || r >= boardSize || c < 0 || c >= boardSize) // boundary validation
                    {
                        break;
                    }

                    if (board[r, c] == 0) // if an empty cell is encountered break loop
                    {
                        break;
                    }

                    if (board[r, c] == currentPlayer) // if the player's own piece is encountered, set valid move to true and break loop
                    {
                        isValidMove = true;
                        break;
                    }
                }
            }

            return isValidMove; // return the final result after passing every condition, either true or false
        }

        private void UpdateBoard()
        {
            blackCount = 0; // set initial counts of both pieces to 0
            whiteCount = 0;

            for (int row = 0; row < boardSize; row++) // nested loops to iterate over each position on the board
            {
                for (int col = 0; col < boardSize; col++)
                {
                    Button button = (Button)Controls.Find("btn_" + row + "_" + col, true).FirstOrDefault(); // locate the button control corresponding with the current board position. Naming convention "btn_row_col"

                    if (button != null) // while a button is present at the current location
                    {
                        if (board[row, col] == 1) // if the cell contains a black piece...
                        {
                            button.BackColor = Color.Black; // set the button colour to black
                            button.Enabled = false; // disable the button so that the user cannot click and overwrite it
                            blackCount++; // increment the count of black pieces 
                        }
                        else if (board[row, col] == 2) // if the cell contains a white piece...
                        {
                            button.BackColor = Color.White; // set the button colour to white
                            button.Enabled = false; // disable the button
                            whiteCount++; // increment the count of white pieces
                        }
                        else 
                        {
                            if (ValidMove(row, col)) // else the cell is empty and a valid move is possible...
                            {
                                button.BackColor = Color.LightGreen; // set the button to light green, as it resembles a cell that a piece can be moved to
                                button.Enabled = true; // enable the button so it can be clicked on
                            }
                            else // the cell is empty and no valid move is possible
                            {
                                button.BackColor = Color.Green; // set the button to green to indicate that no move can be made
                                button.Enabled = false; // disable the button as a move cannot be made to that cell
                            }
                        }
                    }
                }
            }
            player1ScoreLabel.Text = "x " + blackCount.ToString(); // update the score labels to determine the accurate scores of both players
            player2ScoreLabel.Text = "x " + whiteCount.ToString();
        }

        private bool GameOver() // function to determine whether a game is over or should be continued
        {
            for (int row = 0; row < boardSize; row++) // iterate over each position on the board
            {
                for (int col = 0; col < boardSize; col++)
                {
                    if (ValidMove(row, col)) // check if a valid move is possible at the specific location
                    {
                        return false; // if a valid move is found then return false, continue the game
                    }
                }
            }

            return true; // else return true, end the game
        }

        private void GameOverMessage()
        {
            for (int row = 0; row < boardSize; row++) // iterate over each position on the board
            {
                for (int col = 0; col < boardSize; col++)
                {
                    if (board[row, col] == 1)
                    {
                        blackCount++;
                    }
                    else if (board[row, col] == 2)
                    {
                        whiteCount++;
                    }
                }
            }

            if (blackCount > whiteCount) // compare the counts
            {
                MessageBox.Show($"{player1TextBox.Text} wins!"); // if player 1 wins, their name will be displayed stating that they have won
            }
            else if (whiteCount > blackCount)
            {
                MessageBox.Show($"{player2TextBox.Text} wins!"); // else if player 2 wins
            }
            else
            {
                MessageBox.Show("Draw!"); // if neither player 1 nor player 2 have won then a draw commences
            }

            UpdateBoard();
        }

        private void SwitchPlayer()
        {
            currentPlayer = OtherPlayer();
            //statusLabel1.Visible = false; // only needed for debugging to determine if it was correctly determining and switching between the current player
            //statusLabel1.Text = "Current player: " + (currentPlayer == 1 ? "Black" : "White");
            player1PictureBox.Visible = (currentPlayer == 1); // display the picture only when it is player 1's turn
            player2PictureBox.Visible = (currentPlayer == 2); // display the picture only when it is player 2's turn
        }

        private int OtherPlayer()
        {
            return currentPlayer == 1 ? 2 : 1; // switch players
        }

        private void informationPanelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            /*if (informationPanelToolStripMenuItem.Checked)
            {
                infoPanel.Visible = true;
            }
            else
            {
                infoPanel.Visible = false;
            }*/

            infoPanel.Visible = !informationPanelToolStripMenuItem.Checked; // toggle visibility when menu item is clicked
            informationPanelToolStripMenuItem.Checked = infoPanel.Visible;
        }

        private void ClearButtons()
        {
            foreach (Control control in Controls) // iterate over each control in Controls (the buttons)
            {
                Button button = control as Button; // checks if the current control is a Button. If it is, then control is assigned to the variable "button"
                if (button != null) // if a button is present...
                {
                    int row, col;
                    if (int.TryParse(button.Name.Split('_')[1], out row) && int.TryParse(button.Name.Split('_')[2], out col)) // extracts the row and col info. The Split function splits the Name property of the button using an _, creating an array of strings
                     // it then assesses the second [1] and third [2] elements of said array, converting them to integers through parsing. The if statement checks to ensure that they have been successfully parsed
                    {
                        board[row, col] = 0; // reset the value of the board cell
                        button.Enabled = false; // disable the button
                        button.BackColor = Color.Green; // reset the button color to green
                    }
                }
            }

            blackCount = 2; // set counts to 2 as the game starts, due to each player both starting with 2 pieces on the board 
            whiteCount = 2;
        }

        private void gameToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void newGameTab_Click(object sender, EventArgs e)
        {
            player2PictureBox.Visible = false; // if the previous game current player was white, ensure that once a new game is initialised that it is set to black
            player1PictureBox.Visible = true;
            currentPlayer = 1; // set the current player to black every time a new game is initialised
            ClearButtons();
            InitialiseBoard();
            SetBoard();
            UpdateBoard();
            LoadGame();
            blackCount = 2;
            whiteCount = 2;
        }

        private void saveGameTab_Click(object sender, EventArgs e)
        {

            GameData gameData = new GameData(board, currentPlayer, blackCount, whiteCount);

            List<GameData> existingData = new List<GameData>(); // read existing data from the file
            if (File.Exists(savePath))
            {
                string existingJson = File.ReadAllText(savePath);
                existingData = JsonConvert.DeserializeObject<List<GameData>>(existingJson);
            }

            if (existingData.Count < maxSaves)
            {
                existingData.Add(gameData); // if there's space just add the new game state
            }
            else
            {
                GameData selectedState = StateSelection(existingData); // if 5 states are already saved, prompt the user to select one to overwrite
                if (selectedState != null) // if there has been a state selected to be overwritten...
                {
                    existingData[existingData.IndexOf(selectedState)] = gameData; // replace the selected state with the new game state
                }
                else
                {
                    return; // user canceled the selection
                }
            }

            string jsonData = JsonConvert.SerializeObject(existingData); // serialise and write back to the file
            File.WriteAllText(savePath, jsonData);

            MessageBox.Show("Game state saved to:" + Path.GetFullPath(savePath));
        }





        private GameData StateSelection(List<GameData> gameStates)
        {
            Form gameStateSelectionForm = new Form(); // create a new form for game state selection
            gameStateSelectionForm.Text = "Select a game state to overwrite";
            gameStateSelectionForm.StartPosition = FormStartPosition.CenterParent;
            gameStateSelectionForm.AutoSize = true;

            ListBox listBox = new ListBox(); // create a list box to display the game states
            listBox.DataSource = gameStates;
            listBox.DisplayMember = "Description"; // a descriptive string for each game state
            listBox.Dock = DockStyle.Fill;

            Button confirmButton = new Button(); // create a button to confirm the selection
            confirmButton.Text = "OK";
            confirmButton.DialogResult = DialogResult.OK;
            confirmButton.Dock = DockStyle.Bottom;

            gameStateSelectionForm.Controls.Add(listBox); // add the list box and confirm button to the form
            gameStateSelectionForm.Controls.Add(confirmButton);

            DialogResult result = gameStateSelectionForm.ShowDialog(this); // show the form as a dialog

            if (result == DialogResult.OK) // check if the user confirmed the selection
            {
                GameData selectedGameState = (GameData)listBox.SelectedItem; // get the selected game state from the list box
                return selectedGameState;
            }
            else
            {
                return null; // user canceled the selection
            }
        }

        public class GameData // class to declare the data required to be saved
        {
            public int[,] board; // board field, 2d array used to store the board
            public int currentPlayer; // player identifier (1 for black, 2 for white)
            public int blackCount; // to store black count
            public int whiteCount; // to store white count

            public GameData(int[,] board, int currentPlayer, int blackCount, int whiteCount) // constructor for the GameData class, initialises the fields of the class with actual values
            {
                this.board = board;
                this.currentPlayer = currentPlayer;
                this.blackCount = blackCount;
                this.whiteCount = whiteCount;
            }
        }

        private void exitGameTool_Click(object sender, EventArgs e) // upon clicking exit game...
        {
            if (GameInPlay()) // if a game is currently in process...
            {
                DialogResult result = MessageBox.Show("Do you want to save the game before exiting?",
                    "Save Game", MessageBoxButtons.YesNoCancel); // display a messagebox prompting the user if they would like to save the game before exiting

                if (result == DialogResult.Yes) // if they do want to...
                {
                    SaveGame(); // subsequently save the game
                }
                else if (result == DialogResult.Cancel) // else if they haven't pressed no but cancelled out of the message box...
                {
                    return; // exit immediately
                }
            }
            Application.Exit(); // therefore if pressed no, exit from message and suspend game
        }

        private bool GameInPlay() // method for ensuring that the game is in progress
        {
            for (int row = 0; row < boardSize; row++)
            {
                for (int col = 0; col < boardSize; col++)
                {
                    if (ValidMove(row, col))
                    {
                        return true;
                    }
                }
            }

            return false;
        }


        private void SaveGame()
        {
            GameData gameData = new GameData(board, currentPlayer, blackCount, whiteCount);

            List<GameData> existingData = new List<GameData>(); // read existing data from the file
            if (File.Exists(savePath))
            {
                string existingJson = File.ReadAllText(savePath);
                existingData = JsonConvert.DeserializeObject<List<GameData>>(existingJson);
            }

            if (existingData.Count >= maxSaves)
            {
                GameData selectedState = StateSelection(existingData); // if 5 states are already saved, prompt the user to select one to overwrite
                if (selectedState != null)
                {
                    existingData[existingData.IndexOf(selectedState)] = gameData; // replace the selected state with the new game state
                }
                else
                {
                    return; // user canceled the selection
                }
            }
            else
            {
                existingData.Add(gameData); // if there's space, just add the new game state
            }

            string jsonData = JsonConvert.SerializeObject(existingData); // serialise and write back to the file
            File.WriteAllText(savePath, jsonData);

            MessageBox.Show("Game state saved to:" + Path.GetFullPath(savePath));

        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            About_Form about_Form = new About_Form(); // create a new instance of the About form

            about_Form.ShowDialog(); // show the form
        }

        private void LoadGame()
        {
            if(File.Exists(savePath)) // if a file exists at the save path...
            {
                string jsonData = File.ReadAllText(savePath); // read the saved JSON data
                gameStates = JsonConvert.DeserializeObject<List<GameData>>(jsonData); // deserialise and translate JSON into a list of GameData objects 

                if(gameStates.Count == 0) // disable the Restore Game menu item option if there are no saved game states 
                {
                    restoreGameToolStripMenuItem.Enabled = false;
                }
                else // else there are 1 or more game states, meaning the menu item should be showing
                {
                    restoreGameToolStripMenuItem.Enabled = true;
                }
            }
        }

        private void restoreGameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(gameStates.Count == 1) // if there is only one saved game state, restore it
            {
                GameData gameData = gameStates[0]; // referring to the first of the array
                board = gameData.board;
                currentPlayer = gameData.currentPlayer;
                blackCount = gameData.blackCount;
                whiteCount = gameData.whiteCount;

                UpdateBoard();
            }
            else // else there are multiple saved game states...
            {
                GameData selectedState = StateSelection(gameStates); // allow the user to select one

                if(selectedState != null) // restore the game state that has been selected by the user
                {
                    board = selectedState.board;
                    currentPlayer = selectedState.currentPlayer;
                    blackCount = selectedState.blackCount;
                    whiteCount = selectedState.whiteCount;

                    UpdateBoard();
                }
            }
        }
    }
}
