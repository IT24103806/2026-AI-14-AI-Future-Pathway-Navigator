import React, { useState } from 'react';
import ScoreRing from './ScoreRing';
import MarketMeter from './MarketMeter';
import TrendBadge from './TrendBadge';
import MissingSkillsPanel from './MissingSkillsPanel';
import RoadmapTimeline from './RoadmapTimeline';

const PathwayRecommendationCard = ({ recommendation, rank, onBuildPlan, isBuilding }) => {
  const {
    label,
    pathway_name: pathwayName,
    match_score: matchScore,
    demand_score: demandScore,
    competition_score: competitionScore,
    trend,
    data_source: dataSource,
    reasoning,
    missing_skills: missingSkills = [],
    recommended_courses: recommendedCourses = [],
    roadmap = [],
    day_in_the_life: dayInTheLife = '',
    salary_range_lkr: salaryRangeLkr = '',
    industry_tools: industryTools = [],
    portfolio_projects: portfolioProjects = [],
    recommended_certifications: recommendedCertifications = [],
    sri_lankan_education_routes: sriLankanEducationRoutes = [],
  } = recommendation;

  const [isDeepDiveOpen, setIsDeepDiveOpen] = useState(false);

  const isLive = dataSource === 'adzuna_live';
  const hasDeepDive =
    Boolean(dayInTheLife) ||
    industryTools.length > 0 ||
    portfolioProjects.length > 0 ||
    recommendedCertifications.length > 0 ||
    sriLankanEducationRoutes.length > 0;

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

      <div className="pathway-badges-row">
        <span className={`data-source-badge ${isLive ? 'data-source-live' : 'data-source-simulated'}`}>
          {isLive ? '🟢 Live Market Data (Adzuna)' : '🧪 Simulated Market Data'}
        </span>
        {salaryRangeLkr && (
          <span className="salary-range-badge" title="Estimated compensation band">
            💰 {salaryRangeLkr}
          </span>
        )}
      </div>

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

      {hasDeepDive && (
        <div className="career-deep-dive-container">
          <button
            type="button"
            className="deep-dive-toggle-btn"
            aria-expanded={isDeepDiveOpen}
            onClick={() => setIsDeepDiveOpen((prev) => !prev)}
          >
            <span>🔎 Career Deep-Dive, Projects &amp; SL Study Routes</span>
            <span className="deep-dive-chevron">{isDeepDiveOpen ? '▲' : '▼'}</span>
          </button>

          {isDeepDiveOpen && (
            <div className="career-deep-dive-panel">
              {dayInTheLife && (
                <div className="deep-dive-section">
                  <h5>💼 Day in the Life</h5>
                  <p>{dayInTheLife}</p>
                </div>
              )}

              {industryTools.length > 0 && (
                <div className="deep-dive-section">
                  <h5>🛠️ Industry Tools &amp; Tech Stack</h5>
                  <div className="slot-tags-container">
                    {industryTools.map((tool, idx) => (
                      <span key={idx} className="slot-pill tool-pill">{tool}</span>
                    ))}
                  </div>
                </div>
              )}

              {portfolioProjects.length > 0 && (
                <div className="deep-dive-section">
                  <h5>🚀 Starter Portfolio Project Blueprints</h5>
                  <ol className="deep-dive-list">
                    {portfolioProjects.map((project, idx) => (
                      <li key={idx}>{project}</li>
                    ))}
                  </ol>
                </div>
              )}

              {recommendedCertifications.length > 0 && (
                <div className="deep-dive-section">
                  <h5>🏅 Industry Certifications</h5>
                  <ul className="deep-dive-list">
                    {recommendedCertifications.map((cert, idx) => (
                      <li key={idx}>{cert}</li>
                    ))}
                  </ul>
                </div>
              )}

              {sriLankanEducationRoutes.length > 0 && (
                <div className="deep-dive-section">
                  <h5>🎓 Sri Lankan Degree, Foundation &amp; NVQ Routes</h5>
                  <ul className="deep-dive-list sl-routes-list">
                    {sriLankanEducationRoutes.map((route, idx) => (
                      <li key={idx}>{route}</li>
                    ))}
                  </ul>
                </div>
              )}
            </div>
          )}
        </div>
      )}

      <button
        type="button"
        className="btn btn-sm btn-primary"
        onClick={() => onBuildPlan(pathwayName)}
        disabled={isBuilding}
      >
        {isBuilding ? 'Building roadmap...' : 'Build step-by-step roadmap'}
      </button>
    </div>
  );
};

export default PathwayRecommendationCard;
