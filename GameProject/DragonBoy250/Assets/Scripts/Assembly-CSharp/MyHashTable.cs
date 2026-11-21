using System.Collections;

public class MyHashTable
{
	public Hashtable h = new Hashtable();

	public object get(object k)
	{
		if (k == null)
		{
			return null;
		}
		return h[k];
	}

	public void clear()
	{
		h.Clear();
	}

	public IDictionaryEnumerator GetEnumerator()
	{
		return h.GetEnumerator();
	}

	public int size()
	{
		return h.Count;
	}

	public void put(object k, object v)
	{
		if (k == null)
		{
			return;
		}
		h[k] = v;
	}

	public void remove(object k)
	{
		if (k == null)
		{
			return;
		}
		h.Remove(k);
	}

	public void Remove(string key)
	{
		if (key == null)
		{
			return;
		}
		h.Remove(key);
	}

	public bool containsKey(object key)
	{
		if (key == null)
		{
			return false;
		}
		return h.ContainsKey(key);
	}
}
