using System.Collections;
using System.Collections.Generic;
using Unity.Android.Types;
using UnityEngine;

public class ScreenGenerator : MonoBehaviour
{
    public const bool FOVEATE_NO_SPACE = false;

    //number of boxes/pixels to generate for the screen
    public int width = 1;
    public int height = 1;

    //height and width in units of the screen being represented
    public float screenHeight = 100;
    public float screenWidth = 100;

    public GameObject[][] screen = null; //references to the boxes which act as pixels

    public GameObject leftEye = null; //reference to the left eye for collision checks

    public bool showDistance = true;
    
    
    // Start is called before the first frame update
    void Start()
    {
        screen = new GameObject[width][];
        for(int i = 0; i < width; i++)
        {
            screen[i] = new GameObject[height];
        }
        Vector3 tempPos, tempScale;
        float pixelWidth = screenWidth / width;
        float pixelHeight = screenHeight / height;
        for(int i = 0; i < width; i++)
        {
            for(int j = 0; j < height; j++)
            {
                screen[i][j] = GameObject.CreatePrimitive(PrimitiveType.Cube);
                screen[i][j].name = "Pixel " + i + " " + j;
                tempPos = new Vector3((0.5f * pixelWidth) + (pixelWidth * i), (screenHeight - ((0.5f * pixelHeight) + (pixelHeight * j))), 0); //start at half the size of the pixel, then move the distance of one pixel per pixel in the array (move down starting from the top for height)
                tempScale = new Vector3(pixelWidth, pixelHeight, 1);
                screen[i][j].transform.position = tempPos;
                screen[i][j].transform.localScale = tempScale;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        RaycastHit pixelPoint;
        bool hit;
        //shoot a ray forward from the eye
        hit = Physics.Raycast(leftEye.transform.position, leftEye.transform.TransformDirection(Vector3.forward), out pixelPoint, Mathf.Infinity);
        if (hit)
        {
            string pixelName = pixelPoint.collider.gameObject.name;
            //Debug.Log("hit on " + pixelName);
            //take apart name to get the pixel's position in the array
            string[] pixelNums = pixelName.Split(' ');
            int pixelX = int.Parse(pixelNums[1]);
            int pixelY = int.Parse(pixelNums[2]);
            //Debug.Log("hit on: " + pixelNums[0] + " " + pixelX + " " + pixelY);
            //screen[pixelX][pixelY].GetComponent<Renderer>().material.color = Color.red; //mark the main pixel as red

            if (showDistance)
            {
                if (FOVEATE_NO_SPACE)
                {
                    showDistanceAlgorithm(pixelX, pixelY);
                }
                else if (Input.GetKeyDown(KeyCode.Space))
                {
                    showDistanceAlgorithm(pixelX, pixelY);
                }
            }
            else
            {
                if (FOVEATE_NO_SPACE)
                {
                    showFovealAlgorithm(pixelX, pixelY);
                }
                else if (Input.GetKeyDown(KeyCode.Space))
                {
                    showFovealAlgorithm(pixelX, pixelY);
                }
            }
        }
    }

    private void showFovealAlgorithm(int pixelX, int pixelY)
    {
        screen[pixelX][pixelY].GetComponent<Renderer>().material.color = Color.yellow; //mark the main pixel as yellow

        //int i = pixelX;
        //int j = pixelY;

        int ub1, ub2, rb1, rb2, db1, db2, lb1, lb2;
        //bounds define one third and two thirds of the total screen space
        ub1 = pixelY + (int)(0.16f * height);
        ub2 = pixelY + (int)(0.33f * height);
        rb1 = pixelX + (int)(0.16f * height);
        rb2 = pixelX + (int)(0.33f * height);
        db1 = pixelY - (int)(0.16f * height);
        db2 = pixelY - (int)(0.33f * height);
        lb1 = pixelX - (int)(0.16f * height);
        lb2 = pixelX - (int)(0.33f * height);

        //make center pixels green
        for(int i = db1; i < ub1; i++)
        {
            for(int j = lb1; j < rb1; j++)
            {
                if(!(i == pixelY && j == pixelX) && i < height && i >= 0 && j < width && j >= 0)
                {
                    screen[j][i].GetComponent<Renderer>().material.color = Color.green;
                }
            }
        }
        //make the outer regions random colors
        Color randColor;
        for(int i = db2; i < ub2; i += 2)
        {
            for(int j = lb2; j < rb2; j += 2)
            {
                randColor = new Color(Random.value, Random.value, Random.value);
                if(!(i >= db1 && i < ub1 && j >= lb1 && j < rb1) && i < height && i >= 0 && j < width && j >= 0)
                {
                    screen[j][i].GetComponent<Renderer>().material.color = randColor;
                }
                if (!(i+1 >= db1 && i+1 < ub1 && j >= lb1 && j < rb1) && i+1 < ub2 && i+1 < height && i+1 >= 0 && j < width && j >= 0)
                {
                    screen[j][i+1].GetComponent<Renderer>().material.color = randColor;
                }
                if (!(i >= db1 && i < ub1 && j + 1 >= lb1 && j + 1 < rb1) && j + 1 < rb2 && i < height && i >= 0 && j+1 < width && j+1 >= 0)
                {
                    screen[j + 1][i].GetComponent<Renderer>().material.color = randColor;
                }
                if (!(i + 1 >= db1 && i + 1 < ub1 && j + 1 >= lb1 && j + 1 < rb1) && i + 1 < ub2 && j + 1 < rb2 && i+1 < height && i+1 >= 0 && j+1 < width && j+1 >= 0)
                {
                    screen[j + 1][i + 1].GetComponent<Renderer>().material.color = randColor;
                }
            }
        }
    }

    //shows the distance algorithm using a spiral loop from the given starting pixel
    private void showDistanceAlgorithm(int pixelX, int pixelY)
    {
        screen[pixelX][pixelY].GetComponent<Renderer>().material.color = Color.yellow; //mark the main pixel as yellow

        int i = pixelX;
        int j = pixelY;
        //bounds for the loop (up, right, down, left)
        int bu = pixelY + 1;
        int br = pixelX + 1;
        int bd = pixelY - 1;
        int bl = pixelX - 1;
        //direction
        int dir = 0; //0 = right, 1 = down, 2 = left, 3 = up
        //temp vars for distance
        float distX, distY;
        if(bu >= height)//can't start above going right
        {
            if(br >= width)//can't start right going down
            {
                if(bd < 0)//can't start down going left
                {
                    if(bl < 0)//can't start left going up
                    {
                        //only one pixel?
                        return;
                    }
                    else
                    {
                        //start left going up
                        i -= 1;
                        dir = 3;
                    }
                }
                else
                {
                    //start down going left
                    j -= 1;
                    dir = 2;
                }
            }
            else
            {
                //start right going down
                i += 1;
                dir = 1;
            }
        }
        else
        {
            //start up going right
            j += 1;
            dir = 0;
        }

        while (true)
        {
            if (bu >= height)//can't start above going right
            {
                if (br >= width)//can't start right going down
                {
                    if (bd < 0)//can't start down going left
                    {
                        if (bl < 0)//can't start left going up
                        {
                            //we're done then if we go out of bounds
                            if (i >= width || i < 0 || j >= height || j < 0)
                            {
                                return;
                            }
                        }
                    }
                }
            }
            if (i >= width) // hit the right OOB, push us back in (left) 1, send to lower bound, start going left
            {
                Debug.Log("right OOB");
                Debug.Log("index " + i + " " + j);
                i -= 1;
                j = bd;
                if (bd >= 0) //don't push past one out of bounds
                {
                    bd -= 1;
                }
                dir = 2;
                continue;
            }
            if(i < 0) //hit the left OOB
            {
                Debug.Log("left OOB");
                Debug.Log("index " + i + " " + j);
                i += 1;
                j = bu;
                if (bu < height) //don't push past one out of bounds
                {
                    bu += 1;
                }
                dir = 0;
                continue;
            }
            if(j >= height) //hit the top OOB
            {
                Debug.Log("top OOB");
                Debug.Log("index " + i + " " + j);
                j -= 1;
                i = br;
                if (br < width) //don't push past one out of bounds
                {
                    br += 1;
                }
                dir = 1;
                continue;
            }
            if(j < 0) //hit the bottom OOB
            {
                Debug.Log("bottom OOB");
                Debug.Log("index " + i + " " + j);
                j += 1;
                i = bl;
                if(bl >= 0) //don't push past one out of bounds
                {
                    bl -= 1;
                }
                dir = 3;
                continue;
            }

            //color pixel based on distance
            distX = Mathf.Abs(i - pixelX);
            distY = Mathf.Abs(j - pixelY);
            //make distance fraction of total distance
            distX = distX / width;
            distY = distY / height;
            if(distX > 0.33f || distY > 0.33f)
            {
                //third range, color black
                Debug.Log("index pixel " + i + " " + j);
                screen[i][j].GetComponent<Renderer>().material.color = Color.black;
            }
            else if(distX > 0.16f || distY > 0.16f)
            {
                //second range, color red
                screen[i][j].GetComponent<Renderer>().material.color = Color.red;
            }
            else
            {
                //within inner range, color green
                screen[i][j].GetComponent<Renderer>().material.color = Color.green;
            }

            //now move to the next pixel
            switch (dir)
            {
                case 0:
                    Debug.Log("case 0");
                    i += 1;
                    if(i == br)
                    {
                        //hit the right bound, push it out and move down now
                        if (br < width) //don't push past one out of bounds
                        {
                            br += 1;
                        }
                        dir = 1;
                    }
                    break;
                case 1:
                    Debug.Log("case 1");
                    j -= 1;
                    if(j == bd)
                    {
                        //hit the lower bound, push it out and move left now
                        if (bd >= 0) //don't push past one out of bounds
                        {
                            bd -= 1;
                        }
                        dir = 2;
                    }
                    break;
                case 2:
                    Debug.Log("case 2");
                    i -= 1;
                    if (i == bl)
                    {
                        //hit the left bound, push it out and move up now
                        if (bl >= 0) //don't push past one out of bounds
                        {
                            bl -= 1;
                        }
                        dir = 3;
                    }
                    break;
                case 3:
                    Debug.Log("case 3");
                    j += 1;
                    if (j == bu)
                    {
                        //hit the upper bound, push it out and move right now
                        if (bu < height) //don't push past one out of bounds
                        {
                            bu += 1;
                        }
                        dir = 0;
                    }
                    break;
                default:
                    Debug.LogWarning("dir is in an unknown state!");
                    break;
            }
        }
    }
}
