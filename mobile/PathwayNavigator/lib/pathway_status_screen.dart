import 'package:flutter/material.dart';

class PathwayStatusScreen extends StatefulWidget {
  final String studentId;
  const PathwayStatusScreen({super.key, required this.studentId});

  @override
  State<PathwayStatusScreen> createState() => _PathwayStatusScreenState();
}

class _PathwayStatusScreenState extends State<PathwayStatusScreen> {
  // Demo State (Backend API endpoint: /api/CounsellorReview/student/{id}/status)
  bool _isLoading = false;
  String _status = "Pending"; // Pending, Approved, NeedsRevision
  int _feasibilityScore = 78;
  List<String> _missingSkills = ["Python", "OOP Concepts", "Databases"];
  String _counsellorNote = "Please complete a foundational Python certificate before degree commencement.";
  DateTime? _selectedConsultationDate;

  // Device Feature: Date Picker for booking counsellor review session
  Future<void> _pickConsultationDate() async {
    final DateTime? picked = await showDatePicker(
      context: context,
      initialDate: DateTime.now().add(const Duration(days: 1)),
      firstDate: DateTime.now(),
      lastDate: DateTime.now().add(const Duration(days: 30)),
    );
    if (picked != null) {
      setState(() {
        _selectedConsultationDate = picked;
      });
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Consultation booked for: ${picked.toLocal().toString().split(' ')[0]}')),
      );
    }
  }

  Color _getStatusColor() {
    switch (_status) {
      case "Approved":
        return Colors.green;
      case "NeedsRevision":
        return Colors.orange;
      default:
        return Colors.blue;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Pathway Feasibility & Status'),
        backgroundColor: const Color(0xFF0F172A),
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : SingleChildScrollView(
              padding: const EdgeInsets.all(16.0),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Status Badge Card
                  Card(
                    elevation: 2,
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                    child: Padding(
                      padding: const EdgeInsets.all(16.0),
                      child: Row(
                        children: [
                          Icon(Icons.verified_user_outlined, size: 40, color: _getStatusColor()),
                          const SizedBox(width: 16),
                          Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const Text('Approval Status', style: TextStyle(color: Colors.grey, fontSize: 13)),
                              Text(
                                _status,
                                style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold, color: _getStatusColor()),
                              ),
                            ],
                          ),
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(height: 16),

                  // Feasibility Score Card
                  Card(
                    elevation: 2,
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                    child: Padding(
                      padding: const EdgeInsets.all(16.0),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text('AI Reality Check Score', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                          const SizedBox(height: 12),
                          LinearProgressIndicator(
                            value: _feasibilityScore / 100,
                            minHeight: 10,
                            backgroundColor: Colors.grey[200],
                            color: _feasibilityScore > 70 ? Colors.teal : Colors.amber,
                          ),
                          const SizedBox(height: 8),
                          Text('$_feasibilityScore% Feasibility Match', style: const TextStyle(fontWeight: FontWeight.w600)),
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(height: 16),

                  // Missing Skills Section
                  const Text('Identified Skill Gaps', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                  const SizedBox(height: 8),
                  Wrap(
                    spacing: 8.0,
                    runSpacing: 4.0,
                    children: _missingSkills
                        .map((skill) => Chip(
                              avatar: const Icon(Icons.warning_amber_rounded, size: 16, color: Colors.deepOrange),
                              label: Text(skill),
                              backgroundColor: Colors.orange.shade50,
                            ))
                        .toList(),
                  ),
                  const SizedBox(height: 16),

                  // Counsellor Feedback Card
                  if (_counsellorNote.isNotEmpty) ...[
                    Card(
                      color: const Color(0xFFF8FAFC),
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                      child: Padding(
                        padding: const EdgeInsets.all(14.0),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Text('Counsellor Remarks:', style: TextStyle(fontWeight: FontWeight.bold, color: Color(0xFF334155))),
                            const SizedBox(height: 4),
                            Text(_counsellorNote, style: const TextStyle(color: Color(0xFF475569))),
                          ],
                        ),
                      ),
                    ),
                    const SizedBox(height: 16),
                  ],

                  // Device Feature: Book Review Session
                  OutlinedButton.icon(
                    onPressed: _pickConsultationDate,
                    icon: const Icon(Icons.calendar_month),
                    label: Text(_selectedConsultationDate == null
                        ? 'Book Counsellor 1-on-1 Session'
                        : 'Session: ${_selectedConsultationDate.toString().split(' ')[0]}'),
                    style: OutlinedButton.styleFrom(
                      minimumSize: const Size.fromHeight(48),
                    ),
                  ),
                ],
              ),
            ),
    );
  }
}
