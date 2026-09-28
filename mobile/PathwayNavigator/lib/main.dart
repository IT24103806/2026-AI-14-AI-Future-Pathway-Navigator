import 'package:flutter/material.dart';
import 'member4_api.dart';
import 'member4_login_screen.dart';
import 'pathway_status_screen.dart';

void main() => runApp(const MainApp());

class MainApp extends StatefulWidget {
  const MainApp({super.key});
  @override State<MainApp> createState() => _MainAppState();
}

class _MainAppState extends State<MainApp> {
  final api = Member4Api();
  late final Future<bool> hasSession = api.token().then((value) => value != null);
  @override Widget build(BuildContext context) => MaterialApp(
    title: 'Pathway Navigator', debugShowCheckedModeBanner: false,
    theme: ThemeData(colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xFF2563EB)), useMaterial3: true),
    home: FutureBuilder<bool>(future: hasSession, builder: (_, snapshot) {
      if (!snapshot.hasData) return const Scaffold(body: Center(child: CircularProgressIndicator()));
      return snapshot.data! ? PathwayStatusScreen(api: api) : Member4LoginScreen(api: api);
    }),
  );
}
