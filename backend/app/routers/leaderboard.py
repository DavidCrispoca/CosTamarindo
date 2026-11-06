from __future__ import annotations

from fastapi import APIRouter, Depends, Query

from ..deps import CurrentUser, get_repository, optional_user
from ..models import LeaderboardEntryModel, LeaderboardResponse
from ..repositories import Repository

router = APIRouter(prefix="/api", tags=["leaderboard"])


@router.get("/leaderboard", response_model=LeaderboardResponse)
async def leaderboard(
    limit: int = Query(default=5, ge=1, le=20),
    user: CurrentUser | None = Depends(optional_user),
    repo: Repository = Depends(get_repository),
) -> LeaderboardResponse:
    entries = await repo.leaderboard(user.user_id if user else None, limit)
    return LeaderboardResponse(
        entries=[
            LeaderboardEntryModel(
                rank_position=entry.rank_position,
                username=entry.username,
                best_score=entry.best_score,
                is_me=entry.is_me,
            )
            for entry in entries
        ]
    )
