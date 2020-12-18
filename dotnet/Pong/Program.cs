using System;

namespace Pong
{
    interface VisualElement
    {
        void render();

        // todo: following method should take the key pressed as input
        void onKeyPress();
        void onTickElapsed();
    }
    class Paddle: VisualElement
    {
        enum PaddleDirection
        {
            UP,
            DOWN
        }

        int topLeft_x, topLeft_y;
        int height, width;
        PaddleDirection currentDirection;

        public void render()
        {
            throw new NotImplementedException();
        }

        public void onKeyPress()
        {
            throw new NotImplementedException();
        }

        public void onTickElapsed()
        {
            if(currentDirection == PaddleDirection.UP)
            {
                if(!isWithinLimits())
                {
                    // reverse direction
                    currentDirection = PaddleDirection.DOWN;
                }
                else
                {
                    this.moveUp();
                }
            }
            else
            {
                if(isWithinLimits())
                {
                    currentDirection = PaddleDirection.UP;
                }
                else
                {
                    this.moveUp();
                }
            }
        }

        private bool isWithinLimits()
        {
            //todo
            return false;
        }

        private void moveUp()
        {

        }

        private void moveDown()
        {

        }
    }

    class Ball
    {
        // Ball will be square shaped
        int topLeft_x, topLeft_y;
        int size;
    }

    class Pong
    {
        Paddle l, r;
        Ball ball;

        ScoreBoard sb;
        Player p1, p2;

        public Pong()
        {
            this.l = new Paddle();
            this.r = new Paddle();
            this.ball = new Ball();
            this.sb = new ScoreBoard();
            this.p1 = new Player();
            this.p2 = new Player();
        }

        public void render()
        {

        }
    }

    class ScoreBoard
    {
        // todo: print the scores of all the players in rectangular box
        private void drawBox()
        {
            // todo
        }
    }

    class Player
    {
        int score;
    }

    class Program
    {
        static void Main(string[] args)
        {
            Pong game = new Pong();
            
            // todo: kick off 2 threads , one for rendering the game and one for processing inputs
            
            
        }
    }
}
