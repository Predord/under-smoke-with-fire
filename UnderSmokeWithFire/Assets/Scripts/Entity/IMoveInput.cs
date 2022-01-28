using System.Collections.Generic;

public interface IMoveInput 
{
    List<QuadCell> GetPath();

    void MoveStart(QuadCell endCell);
    bool MoveIterationEnd();
    void StartClimbing(bool isSmallCliff);
    bool ActionPointSpend();
    bool IterationEndActionPointSpend(QuadCell currentCell);
    void MoveEnd();
}
