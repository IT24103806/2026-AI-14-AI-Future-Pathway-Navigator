import React from 'react';
import ScoreRing from './ScoreRing';
import MarketMeter from './MarketMeter';
import TrendBadge from './TrendBadge';
import MissingSkillsPanel from './MissingSkillsPanel';
import RoadmapTimeline from './RoadmapTimeline';

const PathwayRecommendationCard = ({ recommendation, rank }) => {
  const {
    label,
    pathway_name: pathwayName,
    match_score: matchScore,
    demand_score: demandScore,
    competition_score: competitionScore,
    trend,
    data_source: dataSource,
    reasoning,
    missing_skills: missingSkills,
    recommended_courses: recommendedCourses,
    roadmap,
  } = recommendation;

  const isLive = dataSource === 'adzuna_live';

  return (
    <div
      className={`pathway-card ${rank === 0 ? 'pathway-card-top' : ''}`}
      style={{ animationDelay: `${rank * 0.12}s` }}
    >
      {rank === 0 && <div className="pathway-card-ribbon">🏆 Best Match</div>}

      <div className="pathway-card-header">
        <div>
          <span className="pathway-label-pill">{label}</span>
          <h3 className="pathway-name">{pathwayName}</h3>
          <TrendBadge trend={trend} />
        </div>
        <ScoreRing score={matchScore} label="Match" />
      </div>

      <div className="pathway-meters">
        <MarketMeter icon="📈" label="Demand" value={demandScore} variant="demand" />
        <MarketMeter icon="⚔️" label="Competition" value={competitionScore} variant="competition" />
      </div>

      <span className={`data-source-badge ${isLive ? 'data-source-live' : 'data-source-simulated'}`}>
        {isLive ? '🟢 Live Market Data (Adzuna)' : '🧪 Simulated Market Data'}
      </span>

      <p className="pathway-reasoning">{reasoning}</p>

      <MissingSkillsPanel skills={missingSkills} />

      <div className="pathway-courses">
        <h4>📚 Recommended Courses</h4>
        <div className="slot-tags-container">
          {recommendedCourses.map((course, idx) => (
            <span key={idx} className="slot-pill course-pill">{course}</span>
          ))}
        </div>
      </div>

      <RoadmapTimeline roadmap={roadmap} />
    </div>
  );
};

export default PathwayRecommendationCard;
