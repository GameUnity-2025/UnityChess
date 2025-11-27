using System.Collections.Generic;
using UnityChess;
using UnityEngine;
using static UnityChess.SquareUtil;

public class VisualPiece : MonoBehaviour
{
    public delegate void VisualPieceMovedAction(
        Square movedPieceInitialSquare,
        Transform movedPieceTransform,
        Transform closestBoardSquareTransform,
        Piece promotionPiece = null
    );
    public static event VisualPieceMovedAction VisualPieceMoved;

    public Side PieceColor;
    public Piece piece;
    public string PieceTypeManual;

    public Square CurrentSquare => StringToSquare(transform.parent.name);

    // 🔥 TẤT CẢ quân cờ sẽ luôn xoay theo hướng nhìn sang TRÁI
    // X = -90 để nằm ngang mặt bàn
    // Y = -90 để quay mặt sang trái
    public static readonly Quaternion LockedWorldRotation = Quaternion.Euler(-90f, 90f, 0f);

    private const float SquareCollisionRadius = 9f;
    private Camera boardCamera;
    private Vector3 piecePositionSS;
    private List<GameObject> potentialLandingSquares;
    private Transform thisTransform;

    private void Start()
    {
        potentialLandingSquares = new List<GameObject>();
        thisTransform = transform;
        boardCamera = Camera.main;

        LockRotation();
    }

    private void LockRotation()
    {
        if (thisTransform != null)
            thisTransform.rotation = LockedWorldRotation;
    }

    public void OnMouseDown()
    {
        if (BoardManager.Instance != null && !BoardManager.Instance.IsUserInputEnabled)
            return;
        if (!enabled || thisTransform == null)
            return;

        if (this.piece == null) return;
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentBoard == null) return;

        HighlightManager.Instance.ClearHighlights();

        Piece currentPieceOnBoard = GameManager.Instance.CurrentBoard[CurrentSquare];
        ICollection<Movement> legalMoves = null;

        if (currentPieceOnBoard != null &&
            GameManager.Instance.TryGetLegalMoves(currentPieceOnBoard, out legalMoves))
        {
            HighlightManager.Instance.ShowHighlights(legalMoves, currentPieceOnBoard.Owner);
        }

        piecePositionSS = boardCamera.WorldToScreenPoint(transform.position);
    }

    private void OnMouseDrag()
    {
        if (BoardManager.Instance != null && !BoardManager.Instance.IsUserInputEnabled)
            return;
        if (enabled && thisTransform != null)
        {
            Vector3 nextPiecePositionSS = new Vector3(
                Input.mousePosition.x,
                Input.mousePosition.y,
                piecePositionSS.z
            );

            thisTransform.position = boardCamera.ScreenToWorldPoint(nextPiecePositionSS);
        }
    }

    public void OnMouseUp()
    {
        if (BoardManager.Instance != null && !BoardManager.Instance.IsUserInputEnabled)
            return;
        if (!enabled || thisTransform == null)
            return;

        HighlightManager.Instance.ClearHighlights();

        potentialLandingSquares.Clear();
        BoardManager.Instance.GetSquareGOsWithinRadius(
            potentialLandingSquares,
            thisTransform.position,
            SquareCollisionRadius
        );

        if (potentialLandingSquares.Count == 0)
        {
            thisTransform.position = thisTransform.parent.position;
            LockRotation();
            return;
        }

        Transform closestSquareTransform = potentialLandingSquares[0].transform;
        float minDist = (closestSquareTransform.position - thisTransform.position).sqrMagnitude;

        for (int i = 1; i < potentialLandingSquares.Count; i++)
        {
            float d = (potentialLandingSquares[i].transform.position - thisTransform.position).sqrMagnitude;
            if (d < minDist)
            {
                minDist = d;
                closestSquareTransform = potentialLandingSquares[i].transform;
            }
        }

        VisualPieceMoved?.Invoke(CurrentSquare, thisTransform, closestSquareTransform);

        // 🔒 EP LẠI HƯỚNG
        LockRotation();
    }

    public string PieceType
    {
        get
        {
            if (piece != null)
            {
                if (piece is Pawn) return "Pawn";
                if (piece is Rook) return "Rook";
                if (piece is Knight) return "Knight";
                if (piece is Bishop) return "Bishop";
                if (piece is Queen) return "Queen";
                if (piece is King) return "King";
            }
            return PieceTypeManual ?? "Unknown";
        }
    }
}
