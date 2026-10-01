import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../../core/constants/routes.dart';
import '../../../core/widgets/common_widgets.dart';
import '../../reality_check/presentation/reality_check_form.dart';
import '../data/career_models.dart';
import '../data/career_repository.dart';
import 'career_controller.dart';
import 'pathway_card.dart';

class CareerDiscoveryScreen extends StatelessWidget {
  const CareerDiscoveryScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return ChangeNotifierProvider<CareerController>(
      create: (context) => CareerController(context.read<CareerRepository>()),
      child: const _CareerBody(),
    );
  }
}

class _CareerBody extends StatelessWidget {
  const _CareerBody();

  @override
  Widget build(BuildContext context) {
    final controller = context.watch<CareerController>();
    final analysis = controller.analysis;

    return Scaffold(
      appBar: AppBar(title: const Text('Career Discovery')),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Text('Discover your career pathways', style: Theme.of(context).textTheme.headlineSmall),
          const SizedBox(height: 8),
          const Text(
            'Agent 2 analyses your saved profile against career and market data to surface your top '
            'pathways - each explained and validated before you see it.',
          ),
          const SizedBox(height: 12),
          const AiDisclosureBanner(),
          const SizedBox(height: 12),
          LoadingButton(
            icon: controller.hasRun ? Icons.refresh : Icons.auto_awesome,
            label: controller.hasRun ? 'Re-run career discovery' : 'Discover my pathways',
            loading: controller.isAnalyzing,
            onPressed: controller.planningPathway != null ? null : controller.analyze,
          ),
          if (controller.error != null) ...[
            const SizedBox(height: 12),
            ErrorBanner(message: controller.error!, onDismiss: controller.clearError),
            if (controller.needsOnboarding)
              TextButton(
                onPressed: () => context.push(AppRoutes.onboarding),
                child: const Text('Complete onboarding first'),
              ),
          ],
          const SizedBox(height: 16),
          if (controller.isAnalyzing)
            const Padding(
              padding: EdgeInsets.symmetric(vertical: 24),
              child: Column(
                children: [
                  Text('Analysing your profile against market data…'),
                  SizedBox(height: 4),
                  Text(
                    'Matching skills → market signals → explanations → validation. This can take a minute.',
                    textAlign: TextAlign.center,
                  ),
                ],
              ),
            )
          else if (analysis == null)
            const _HowItWorks()
          else
            ..._results(context, controller, analysis),
        ],
      ),
    );
  }

  List<Widget> _results(BuildContext context, CareerController controller, PathwayAnalysis analysis) {
    if (analysis.failed) {
      return [
        ErrorBanner(message: 'Analysis could not be validated: ${analysis.validationErrors.join(' ')}'),
      ];
    }
    final plan = controller.plan;
    return [
      Text('Your top ${analysis.recommendations.length} pathways', style: Theme.of(context).textTheme.titleLarge),
      Text('Workflow: ${analysis.workflowId}', style: Theme.of(context).textTheme.bodySmall),
      const SizedBox(height: 12),
      _DecisionPanel(controller: controller, analysis: analysis),
      const SizedBox(height: 12),
      for (var i = 0; i < analysis.recommendations.length; i++)
        PathwayCard(
          recommendation: analysis.recommendations[i],
          rank: i,
          isBuilding: controller.planningPathway == analysis.recommendations[i].pathwayName,
          planningBusy: controller.planningPathway != null,
          onBuildPlan: () => controller.buildPlan(analysis.recommendations[i].pathwayName),
        ),
      if (plan != null) _PlanView(plan: plan),
      RealityCheckForm(
        // A new analysis must reset the form state.
        key: ValueKey(analysis.id ?? analysis.workflowId),
        analysisId: analysis.id,
        careers: [for (final r in analysis.recommendations) r.pathwayName],
      ),
    ];
  }
}

class _HowItWorks extends StatelessWidget {
  const _HowItWorks();

  static const List<(IconData, String, String)> steps = [
    (Icons.track_changes, 'Match', 'Scores your skills, interests and ambitions against a curated career knowledge base.'),
    (Icons.bar_chart, 'Market data', 'Pulls job-market demand and competition signals for each candidate pathway.'),
    (Icons.psychology_alt_outlined, 'AI reasoning', 'Explains each match in plain language, then validates every result.'),
  ];

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        for (var i = 0; i < steps.length; i++)
          Card(
            child: ListTile(
              leading: Icon(steps[i].$1),
              title: Text('${i + 1}. ${steps[i].$2}'),
              subtitle: Text(steps[i].$3),
            ),
          ),
      ],
    );
  }
}

class _DecisionPanel extends StatelessWidget {
  const _DecisionPanel({required this.controller, required this.analysis});

  final CareerController controller;
  final PathwayAnalysis analysis;

  @override
  Widget build(BuildContext context) {
    if (analysis.isDecided) {
      final approved = analysis.status == AnalysisStatus.approved;
      return Card(
        child: ListTile(
          leading: Icon(approved ? Icons.check_circle_outline : Icons.cancel_outlined),
          title: Text('You ${approved ? 'approved' : 'rejected'} this analysis'),
        ),
      );
    }
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Reviewed all pathways? Record your decision:'),
            const SizedBox(height: 8),
            Wrap(
              spacing: 8,
              children: [
                FilledButton.icon(
                  onPressed: controller.isDeciding || analysis.id == null ? null : controller.approve,
                  icon: const Icon(Icons.check),
                  label: const Text('Approve'),
                ),
                OutlinedButton.icon(
                  onPressed: controller.isDeciding || analysis.id == null ? null : controller.reject,
                  icon: const Icon(Icons.close),
                  label: const Text('Reject'),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _PlanView extends StatelessWidget {
  const _PlanView({required this.plan});

  final PathwayPlan plan;

  @override
  Widget build(BuildContext context) {
    return SectionCard(
      title: '${plan.selectedPathway} roadmap',
      icon: Icons.map_outlined,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Next action: ${plan.nextAction}'),
          const SizedBox(height: 12),
          for (final step in plan.roadmap)
            Padding(
              padding: const EdgeInsets.only(bottom: 12),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('${step.order}. ${step.title}', style: Theme.of(context).textTheme.titleSmall),
                  Text(step.outcome),
                  if (step.actions.isNotEmpty) Text(step.actions.join(' ')),
                  Text('Estimated: ${step.estimatedDuration}', style: Theme.of(context).textTheme.bodySmall),
                ],
              ),
            ),
        ],
      ),
    );
  }
}
