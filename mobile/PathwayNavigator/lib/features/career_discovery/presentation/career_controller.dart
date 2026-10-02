import 'package:flutter/foundation.dart';

import '../../../core/error/app_exception.dart';
import '../data/career_models.dart';
import '../data/career_repository.dart';

/// State for the Career Discovery screen (Agents 2 and 3).
class CareerController extends ChangeNotifier {
  CareerController(this._repository);

  final CareerRepository _repository;

  PathwayAnalysis? _analysis;
  PathwayPlan? _plan;
  bool _loadingSaved = false;
  bool _analyzing = false;
  bool _deciding = false;
  String? _planningPathway;
  String? _updatingStage;
  String? _error;
  bool _needsOnboarding = false;

  PathwayAnalysis? get analysis => _analysis;
  PathwayPlan? get plan => _plan;
  bool get isLoadingSaved => _loadingSaved;
  bool get isAnalyzing => _analyzing;
  bool get isDeciding => _deciding;
  String? get planningPathway => _planningPathway;
  String? get updatingStage => _updatingStage;
  String? get error => _error;

  /// True when the backend said the profile is incomplete - the UI offers a shortcut to onboarding.
  bool get needsOnboarding => _needsOnboarding;
  bool get hasRun => _analysis != null;

  void clearError() {
    _error = null;
    _needsOnboarding = false;
    notifyListeners();
  }

  void _fail(Object error, String fallback) {
    _error = describeError(error, fallback: fallback);
    _needsOnboarding = _error!.toLowerCase().contains('complete onboarding');
  }

  /// Auto-loads the student's latest saved Agent 2 analysis and Agent 3 roadmap.
  Future<void> loadSaved() async {
    if (_loadingSaved) return;
    _loadingSaved = true;
    notifyListeners();
    try {
      final results = await Future.wait<Object?>([
        _repository.getLatestAnalysis(),
        _repository.getLatestPlan(),
      ]);
      final savedAnalysis = results[0] as PathwayAnalysis?;
      final savedPlan = results[1] as PathwayPlan?;
      if (savedAnalysis != null) {
        _analysis = savedAnalysis;
      }
      if (savedPlan != null && savedPlan.isReady) {
        _plan = savedPlan;
      }
    } catch (_) {
      // Initial auto-load is best-effort; do not block the student from running a fresh analysis.
    } finally {
      _loadingSaved = false;
      notifyListeners();
    }
  }

  Future<void> analyze() async {
    if (_analyzing) return;
    _analyzing = true;
    _error = null;
    _needsOnboarding = false;
    notifyListeners();
    try {
      _analysis = await _repository.analyze();
    } catch (error) {
      _fail(error, 'Something went wrong while analysing your pathways.');
    } finally {
      _analyzing = false;
      notifyListeners();
    }
  }

  Future<void> buildPlan(String pathwayName) async {
    if (_planningPathway != null) return;
    _planningPathway = pathwayName;
    _error = null;
    notifyListeners();
    try {
      final existingPhases = (_plan != null && _plan!.selectedPathway.toLowerCase() == pathwayName.toLowerCase())
          ? _plan!.completedPhases
          : const <String>[];
      final plan = await _repository.buildPlan(pathwayName, completedPhases: existingPhases);
      if (plan.isReady) {
        _plan = plan;
      } else {
        final reasons = plan.validationErrors.join(' ');
        _error = 'Roadmap could not be built: ${reasons.isEmpty ? 'unknown error.' : reasons}';
      }
    } catch (error) {
      _fail(error, 'Something went wrong while building the roadmap.');
    } finally {
      _planningPathway = null;
      notifyListeners();
    }
  }

  Future<void> toggleStage(String stage) async {
    final currentPlan = _plan;
    if (currentPlan == null || _updatingStage != null) return;
    _updatingStage = stage;
    _error = null;
    notifyListeners();

    final currentCompleted = currentPlan.completedPhases.isNotEmpty
        ? currentPlan.completedPhases
        : [for (final s in currentPlan.roadmap) if (s.isCompleted) s.stage];

    final lower = stage.toLowerCase();
    final nextCompleted = currentCompleted.any((p) => p.toLowerCase() == lower)
        ? [for (final p in currentCompleted) if (p.toLowerCase() != lower) p]
        : <String>[...currentCompleted, stage];

    try {
      final updated = currentPlan.id != null
          ? await _repository.updatePlanProgress(currentPlan.id!, nextCompleted)
          : await _repository.buildPlan(currentPlan.selectedPathway, completedPhases: nextCompleted);
      if (updated.isReady) {
        _plan = updated;
      }
    } catch (error) {
      _fail(error, 'Failed to save roadmap milestone progress.');
    } finally {
      _updatingStage = null;
      notifyListeners();
    }
  }

  Future<void> approve() => _decide(_repository.approve);

  Future<void> reject() => _decide(_repository.reject);

  Future<void> _decide(Future<PathwayAnalysis> Function(String id) action) async {
    final id = _analysis?.id;
    if (id == null || _deciding) return;
    _deciding = true;
    _error = null;
    notifyListeners();
    try {
      _analysis = await action(id);
    } catch (error) {
      _fail(error, 'Failed to record your decision.');
    } finally {
      _deciding = false;
      notifyListeners();
    }
  }
}
