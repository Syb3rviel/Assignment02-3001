using System.Collections;
using System.Collections.Generic;
using System.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
using Utils;

public class GridScript : MonoBehaviour
{
    [SerializeField] GameObject tilePrefab;
    [SerializeField] GameObject costEnabler;
    [SerializeField] GameObject planePrefab;
    //[SerializeField] GameObject costPrefab;

    GameObject mouseObj;

    const int rows = 8;
    const int cols = 12;

    //things to do with upping the iterations
    [SerializeField] int iterations;
    float itTime;
    float itDuration = 0.04f;
    public bool iterationsDone;

    //timing for a star to showcase before the object moves along the path
    float pauseTime;
    float pauseDuration = 1.0f;

    //start should be red and end should be green
    Cell start = new Cell();
    Cell end = new Cell();

    //bools for things should go here
    bool[] startEndSelect = { false, false };
    public bool isDebugDraw = true;
    public bool started;
    bool testStart;

    List<List<GameObject>> tileObjects = new List<List<GameObject>>();

    //path following
    List<Vector3> waypoints = new List<Vector3>();
    int curr = 0;
    int next = 1;
    float t = 0.0f;
    GameObject pathFollower;


    //double arrays -- using the same type of thing for the costs to make it easier to find them on the canvas
    //so changing the text isnt so weird
    [SerializeField]
    TMP_Text[] costTileArray;
    TMP_Text[,] costTiles = new TMP_Text[8, 12];

    int[,] tiles =
    {  //0  1  2  3  4  5  6  7  8  9  10  11   collumns
        {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},  //0
        {1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},  //1
        {1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},  //2
        {1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},  //3
        {1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},  //4
        {1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},  //5
        {1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},  //6
        {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1}   //7
    };                                         //rows
        

    void Start()
    {
        float x = 0.5f;
        float y = 7.5f;
        for (int row = 0; row < rows; row++)
        {
            List<GameObject> rowObjects = new List<GameObject>();
            for (int col = 0; col < cols; col++)
            {
                GameObject tile = Instantiate(tilePrefab);
                tile.transform.position = new Vector3(x, y);
                rowObjects.Add(tile);
                x += 1.0f;
            }
            tileObjects.Add(rowObjects);
            y -= 1.0f;
            x = 0.5f;
        }

        RandomizeTileDraw();

        mouseObj = Instantiate(tilePrefab);
        mouseObj.GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, 0.5f);

        //priority queue
        PriorityQueue<int, float> pq = new PriorityQueue<int, float>();

        costEnabler.SetActive(false);
        int counter = 0;
        for (int row = 0; row < rows; row++)
        {
            for(int col = 0; col < cols; col++)
            {
                costTiles[row, col] = costTileArray[counter];
                counter++;
                //Debug.Log(counter);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        DrawMouse();

        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            ResetScene();
        }
        if (Input.GetKeyDown(KeyCode.Alpha1) && !isDebugDraw)
        {
            isDebugDraw = true;
            costEnabler.SetActive(true);
        }
        else if(Input.GetKeyDown(KeyCode.Alpha1) && isDebugDraw)
        {
            isDebugDraw = false;
            costEnabler.SetActive(false);
        }


        if (!startEndSelect[0] || !startEndSelect[1])
        {
            SelectPositions();
        }
        else if(!started && startEndSelect[0] && startEndSelect[1])
        {
            waypoints.Add(GridToWorld(start));
            pathFollower = GameObject.Instantiate(planePrefab);
            pathFollower.transform.position = waypoints[curr];
            
            started = true;
        }


        if (started)
        {
            startTileFollow();
        }
    }

    void startTileFollow()
    {
        itTime += Time.deltaTime;
        pauseTime += Time.deltaTime;
        if (itDuration < itTime && !iterationsDone)
        {
            iterations++;
            itTime = 0;
            pauseTime = 0;
        }
        if (pauseDuration < pauseTime)
        {
            testStart = true;
        }

        List<Cell> path = A_Star.aStar(start, end, tiles, iterations, this);
        path.Reverse();

        foreach (Cell cell in path)
        {
            waypoints.Add(GridToWorld(cell));
            DrawCell(cell, Color.red);
        }

        if (testStart)
        {
            List<Cell> testPath = new List<Cell>();
            foreach (Vector3 pos in waypoints)
            {
                testPath.Add(WorldToGrid(pos));
            }

            foreach (Cell cell in testPath)
                DrawCell(cell, Color.yellow);

            if (!(curr >= path.Count))
            {
                //TODO:: increment and bound curr and next indices when time epxires
                t += Time.deltaTime;
                if (t > 1.0f)
                {
                    curr++;
                    next++;
                    t = 0.0f;
                }

                //lab 5 TODO: use a path generated by dijkstra instead of testpath
                Vector3 pathPosition = Vector3.Lerp(waypoints[curr], waypoints[next], t);
                Vector3 directionToTarget = waypoints[next] - waypoints[curr];
                directionToTarget = Quaternion.Euler(0, 0, -90) * directionToTarget; //90 degree offset to make it look the right way
                directionToTarget.z = 0; //we will set this to 0 so it doesnt mess up any possible outcomes
                float targetAngle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;
                float currentAngle = pathFollower.transform.rotation.eulerAngles.z; //get both the target and our rotation on z only
                                                                                    //before we were rotating on all of them and it was super wonky
                float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, 1.0f); //move towards to show what it should become

                pathFollower.transform.rotation = Quaternion.Euler(0, 0, newAngle); //only use z 
                pathFollower.transform.position = pathPosition;
            }

        }
    }
    void SelectPositions()
    {
        Vector2 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Cell mouseCell = WorldToGrid(mouse);

        //you can also press f before you lay down your start and finish to get a different layout of obstacles
        if (!startEndSelect[0] && !startEndSelect[1] && Input.GetKeyDown(KeyCode.F))
        {
            RandomizeTileDraw();
        }

        //you can select either the start and the finish, but can only select each one time
        if (Input.GetMouseButtonDown(0) && !startEndSelect[0])
        {
            start.row = mouseCell.row;
            start.col = mouseCell.col;
            DrawCell(start, Color.red);
            startEndSelect[0] = true;
        }
        if (Input.GetMouseButtonDown(1) && !startEndSelect[1])
        {
            end.row = mouseCell.row;
            end.col = mouseCell.col;
            DrawCell(end, Color.green);
            startEndSelect[1] = true;
        }
    }
    void RandomizeTileDraw()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                if(row == 0 || row == 7 || col == 0 || col == 11)
                {
                    continue;
                }

                //randomize whether theres an obstacle in that spot or not, 
                //lower chance for the obstacles to spawn then the open area though
                int ran = Random.Range(0, 4);

                if(ran < 3)
                {
                    tiles[row, col] = 0;
                }
                else
                {
                    tiles[row, col] = 1;
                }
            }
        }

        DrawTiles();
    }

    void ResetScene()
    {
        //Debug.Log("PRessed");
        RandomizeTileDraw();
        startEndSelect[0] = false;
        startEndSelect[1] = false;
        started = false;
        iterationsDone = false;
        testStart = false;
        iterations = 0;
        curr = 0;
        next = 1;
        pauseTime = 0;
        waypoints.Clear();
        waypoints.TrimExcess();
        Destroy(pathFollower); pathFollower = null;
    }
    void DrawTiles()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                Cell cell = new Cell { row = row, col = col };
                int type = tiles[row, col];
                DrawCell(cell, TileColor(type));
            }
        }
    }

    public Color TileColor(int type)
    {
        Color[] colors = new Color[2];
        colors[0] = Color.grey;
        colors[1] = Color.blue;
       
        return colors[type];
    }
    public void DrawCell(Cell cell, Color color)
    {
        GameObject obj = tileObjects[cell.row][cell.col];
        obj.GetComponent<SpriteRenderer>().color = color;
    }

    public void ShowCosts(Node node, int cost, int row, int col)
    {
        //here we will turn on and discover what each cost is written down from each cell
        //some of the costs are way too high and take up too much room so the highest will be 2147
        if(cost > 2147)
        {
            cost = 2147;
        }
        costTiles[row, col].text = "Cost:\n" + cost;
    }

    void DrawMouse()
    {
        Vector2 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Cell mouseCell = WorldToGrid(mouse);
        //Debug.Log("Row: " + mouseCell.row + ", Col: " + mouseCell.col);

        if (!Cell.Equals(mouseCell, Cell.Invalid()))
        {
            //DrawCell(mouseCell, Color.cyan);
            mouseObj.transform.position = GridToWorld(mouseCell);
        }
        
    }

    public int GetHeuristic(Cell a, Cell b)
    {
        return Mathf.Abs(a.row - b.row) + Mathf.Abs(a.col - b.col);
    }

    public int TileCosts(int type)
    {
        int[] costs = new int[2];
        costs[0] = 1;
        costs[1] = 100;
        return costs[type];
    }
    public int TileType(Cell cell)
    {
        return tiles[cell.row, cell.col];
    }
    Vector3 GridToWorld(Cell cell)
    {
        float x = (float)cell.col + 0.5f;
        float y = (float)(rows - 1 - cell.row) + 0.5f;
        return new Vector3(x, y, 0.0f);
    }
    Cell WorldToGrid(Vector3 pos)
    {
        if (pos.x < 0.0f || pos.x > cols || pos.y < 0.0f || pos.y > rows)
        {
            return Cell.Invalid();
        }

        Cell cell = new Cell();
        cell.col = (int)pos.x;
        cell.row = (rows - 1) - (int)pos.y;
        return cell;
    }
}
