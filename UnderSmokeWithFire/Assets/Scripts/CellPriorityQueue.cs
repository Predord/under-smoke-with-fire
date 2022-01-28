using System.Collections.Generic;
using System.Linq;

public class CellPriorityQueue
{
    private Dictionary<float, QuadCell> dictionary = new Dictionary<float, QuadCell>();

    public int Count
    {
        get
        {
            return dictionary.Count;
        }
    }

    public void Enqueue(QuadCell cell)
    {
        float priority = cell.SearchPriority;

        if(dictionary.TryGetValue(priority, out QuadCell cellWithSamePriority))
        {
            cell.NextWithSamePriority = cellWithSamePriority;
            dictionary[priority] = cell;
        }
        else
        {
            cell.NextWithSamePriority = null;
            dictionary.Add(priority, cell);
        }
    }

    public QuadCell Dequeue()
    {
        float minimum = dictionary.Keys.Min();
        if (dictionary.TryGetValue(minimum, out QuadCell cell))
        {
            if (cell.NextWithSamePriority)
            {
                dictionary[minimum] = cell.NextWithSamePriority;
            }
            else
            {
                dictionary.Remove(minimum);
            }

            return cell;
        }
        return null;
    }

    public void Change(QuadCell cell, float oldPriority)
    {
        if(dictionary.TryGetValue(oldPriority, out QuadCell current))
        {
            if (current == cell)
            {
                if (current.NextWithSamePriority)
                {
                    dictionary[oldPriority] = current.NextWithSamePriority;
                }
                else
                {
                    dictionary.Remove(oldPriority);
                }
            }
            else
            {
                QuadCell next = current.NextWithSamePriority;
                while (next != cell)
                {
                    current = next;
                    next = current.NextWithSamePriority;
                }
                current.NextWithSamePriority = cell.NextWithSamePriority;
            }
            Enqueue(cell);
        }
    }

    public void Clear()
    {
        dictionary.Clear();
    }
}
