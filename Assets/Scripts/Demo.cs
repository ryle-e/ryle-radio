using Unity.VisualScripting.FullSerializer;

public class Demo
{

    public void Demo1()
    {
        int thing = 0;

        if (thing == 0)
        {
            // thing is 0
        }
        else if (thing == 1)
        {
            // thing is 1
        }
        else if (thing == 2)
        {
            // thing is 2
        }
        else
        {
            // thing is something else
        }


        switch (thing)
        {
            case 0:
                // thing is 0
                print("this is 0");
                break;

            case 1:
                // thing is 1
                print("this number is above 0");

            case 2:
                // thing is 2
                print("this is 2");
                break;

            default:
                // thing is something else
        }
    }

}