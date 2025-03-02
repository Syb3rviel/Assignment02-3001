using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;

public struct Cell
{
    public int row;
    public int col;
    //public int cost;

    public static bool Equals(Cell a, Cell b)
    {
        return a.row == b.row && a.col == b.col;
    }
    public static Cell Invalid()
    {
        return new Cell { row = -1, col = -1 };
    }
}

public struct Node
{
    public Cell curr; //current cell
    public Cell prev; //parent(cell before current
    //public Vector2 connection;
    public int Fcost; //how expensive it is to move to this node
    public int Gcost;
    public int Hcost;
}
public class A_Star : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public static List<Cell> aStar(Cell start, Cell end, int[,] tiles, int iterations, GridScript grid)
    {
        int rows = tiles.GetLength(0);
        int cols = tiles.GetLength(1);
        bool[,] closed = new bool[rows, cols];  //<--- Cells we've already explored(cant explore again otherwise infinite loop)
        Node[,] nodes = new Node[rows, cols];  ///<--- connections between cells(each cell and what came before each cell
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                closed[row, col] = tiles[row, col] == 1;
                nodes[row, col].curr = new Cell { row = row, col = col };
                nodes[row, col].prev = Cell.Invalid();
                nodes[row, col].Gcost = int.MaxValue;
                nodes[row, col].Hcost = int.MaxValue;
                nodes[row, col].Fcost = int.MaxValue;
            }
        }

        PriorityQueue<Cell, float> open = new PriorityQueue<Cell, float>();
        open.Enqueue(start, 0.0f);
        nodes[start.row, start.col].Fcost = 0;
        //nodes[end.row, end.col].Fcost = 0;

        HashSet<Cell> debugCell = new HashSet<Cell>();

        bool found = false;
        for (int i = 0; i < iterations; i++)
        {
            //examine the cell with the highest priority(lowest cost)
            Cell front = open.Dequeue();
            //Debug.Log("Checking cell: (" + front.row + ", " + front.col + ") with Fcost: " + nodes[front.row, front.col].Fcost);

            //stop searching if weve reached our goal
            if (Cell.Equals(front, end))
            {
                grid.iterationsDone = true;
                found = true;
                break;
            }

            //if (grid.isDebugDraw)
            debugCell.Add(front);

            //update cell cost and add it to open list if the new cost is cheaper then the old cost
            foreach (Cell adj in Adjacent(front, rows, cols))
            {
                int prevGcost = nodes[front.row, front.col].Gcost;
                int newGcost = grid.GetHeuristic(start, adj);

                int hCost = grid.GetHeuristic(adj, end);

                int prevCost = nodes[adj.row, adj.col].Fcost;
                
                int currCost = newGcost + hCost + grid.TileCosts(grid.TileType(adj));

                if (currCost < prevCost)
                {
                    open.Enqueue(adj, currCost);
                    nodes[adj.row, adj.col].Fcost = currCost;
                    nodes[adj.row, adj.col].Gcost = newGcost;
                    nodes[adj.row, adj.col].Hcost = hCost;
                    nodes[adj.row, adj.col].prev = front;
                }
            }
        }

        if (grid.isDebugDraw)
        {
            //here we will call the show costs
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    grid.ShowCosts(nodes[row, col], nodes[row, col].Fcost, row, col);
                }
            }
        }

        if (!found)
        {
            foreach (Cell cell in debugCell)
            {
                if(grid.isDebugDraw)
                    grid.DrawCell(cell, Color.magenta);

                if (!grid.isDebugDraw)
                    grid.DrawCell(cell, new Color(1, 0, 1, 0.2f));
            }
        }

        //if weve found the end, retrace our steps, optherwise theres no solution so return an empty list
        List<Cell> result = found ? Retrace(nodes, start, end, grid) : new List<Cell>();

        return result;
    }

    static List<Cell> Retrace(Node[,] nodes, Cell start, Cell end, GridScript grid)
    {
        List<Cell> path = new List<Cell>();
        //start at the end and work backwords till we reach the start
        Cell curr = end;

        //prev is the cell that came before the current cell
        Cell prev = nodes[curr.row, curr.col].prev;

        for (int i = 0; i < 32; i++)
        {
            path.Add(curr);

            curr = prev;

            prev = nodes[curr.row, curr.col].prev;
            //Debug.Log("curr row: " + curr.row + ", curr col: " + curr.col);

            //if the previous cell is invalid, menaing theres no previous cell, then weve reached the start!
            if (Cell.Equals(prev, Cell.Invalid()))
            {
                //Debug.Log("Found start");
                break;
            }
        }
        grid.iterationsDone = true;

        return path;
    }

    public static List<Cell> Adjacent(Cell cell, int rows, int cols)
    {
        List<Cell> cells = new List<Cell>();

        // Diagonal: Top-left
        Cell topLeft = new Cell { row = cell.row - 1, col = cell.col - 1 };
        if (topLeft.row >= 0 && topLeft.col >= 0)
        {
            cells.Add(topLeft);
        }

        // Diagonal: Top-right
        Cell topRight = new Cell { row = cell.row - 1, col = cell.col + 1 };
        if (topRight.row >= 0 && topRight.col < cols)
        {
            cells.Add(topRight);
        }

        // Diagonal: Bottom-left
        Cell bottomLeft = new Cell { row = cell.row + 1, col = cell.col - 1 };
        if (bottomLeft.row < rows && bottomLeft.col >= 0)
        {
            cells.Add(bottomLeft);
        }

        // Diagonal: Bottom-right
        Cell bottomRight = new Cell { row = cell.row + 1, col = cell.col + 1 };
        if (bottomRight.row < rows && bottomRight.col < cols)
        {
            cells.Add(bottomRight);
        }


        //TODO - add left of cell if within grid bounds
        Cell left = new Cell { row = cell.row, col = cell.col - 1 };
        if (left.col >= 0)
        {
            cells.Add(left);
        }
        //TODO - add right of cell if within grid bounds
        Cell right = new Cell { row = cell.row, col = cell.col + 1 };
        if (right.col <= 19)
        {
            cells.Add(right);
        }
        //TODO - add up of cell if within grid bounds
        Cell up = new Cell { row = cell.row - 1, col = cell.col };
        if (up.row >= 0)
        {
            cells.Add(up);
        }
        //TODO - add down of cell if within grid bounds
        Cell down = new Cell { row = cell.row + 1, col = cell.col };
        if (down.row <= 9)
        {
            cells.Add(down);
        }

        return cells;
    }
}
