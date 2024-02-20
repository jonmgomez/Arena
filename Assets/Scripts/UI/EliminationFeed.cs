using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EliminationFeed : MonoBehaviour
{
    [SerializeField] EliminationFeedEntry entryPrefab;
    [SerializeField] float entrySpacing = 50f;
    [SerializeField] float entryLifetime = 5f;
    [SerializeField] int maxEntries = 5;

    readonly List<EliminationFeedEntry> entries = new();

    public void AddEliminationEntry(string playerEliminating, string playerDead)
    {
        EliminationFeedEntry entry = Instantiate(entryPrefab, transform);
        entry.transform.localPosition = new Vector3(0f, -entries.Count * entrySpacing, 0f);
        entry.SetNames(playerEliminating, playerDead);

        entries.Add(entry);
        StartCoroutine(RemoveEntryAfterDelay(entry));

        if (entries.Count > maxEntries)
            RemoveOldestEntry();
    }

    private IEnumerator RemoveEntryAfterDelay(EliminationFeedEntry entry)
    {
        yield return new WaitForSeconds(entryLifetime);
        if (entries.Contains(entry))
        {
            entries.Remove(entry);
            Destroy(entry.gameObject);
            ShiftEntries();
        }
    }

    private void RemoveOldestEntry()
    {
        Destroy(entries[0].gameObject);
        entries.RemoveAt(0);
        ShiftEntries();
    }

    private void ShiftEntries()
    {
        for (int i = 0; i < entries.Count; i++)
        {
            entries[i].transform.localPosition += new Vector3(0f, entrySpacing, 0f);
        }
    }
}
