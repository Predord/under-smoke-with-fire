using UnityEngine;

public class QuadCellEffects : MonoBehaviour
{
    public QuadCell cell;
    public ParticleSystem fire;
    public ParticleSystem _light;
    public ParticleSystem smoke;

    public Color visibleFireColor;
    public Color notVisibleFireColor;

    public void InstantiateEffect(QuadCell cell)
    {
        this.cell = cell;
        transform.SetParent(cell.transform, false);
    }

    public void ClearEffects()
    {
        fire.Stop();
        _light.Stop();
        smoke.Stop();

        Destroy(gameObject, Mathf.Max(fire.main.startLifetimeMultiplier, _light.main.startLifetimeMultiplier, smoke.main.startLifetimeMultiplier));
    }

    public void StartFire()
    {
        smoke.Stop();

        if (cell.IsVisible)
        {
            var main = fire.main;
            main.startColor = visibleFireColor;
            fire.Play();
        }
        else if (cell.IsExplored)
        {
            var main = fire.main;
            main.startColor = notVisibleFireColor;
            fire.Play();
        }
    }

    public void SetFireColor(bool cellVisible)
    {
        var main = fire.main;
        main.startColor = cellVisible ? visibleFireColor : notVisibleFireColor;
        fire.Play();
        smoke.Stop();
    }

    public void StartSmoke()
    {
        fire.Stop();
        _light.Stop();

        smoke.Play();  
    }
}
