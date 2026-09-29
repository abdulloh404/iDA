import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../app/app_controller.dart';
import '../../data/demo_repository.dart';
import '../../l10n/strings.dart';
import '../../ui/components.dart';
import 'auth_common.dart';
import 'forgot_password_screen.dart';
import 'otp_screen.dart';


class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _user = TextEditingController();
  final _pass = TextEditingController();
  final _passFocus = FocusNode();
  bool _remember = true;
  bool _busy = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    final saved = AppScope.read(context).rememberedUsername;
    if (saved != null) _user.text = saved;
    _remember = saved != null || _user.text.isEmpty;
  }

  @override
  void dispose() {
    _user.dispose();
    _pass.dispose();
    _passFocus.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final s = S.of(context);
    final username = _user.text.trim();
    if (username.isEmpty || _pass.text.isEmpty) {
      setState(() => _error = '${s.username} / ${s.password}: ${s.required}');
      return;
    }
    FocusScope.of(context).unfocus();
    setState(() {
      _busy = true;
      _error = null;
    });
    final app = AppScope.read(context);
    try {
      final challenge = await app.repo.signIn(username, _pass.text);
      app.rememberUsername(_remember ? username.toUpperCase() : null);
      if (!mounted) return;
      _pass.clear();
      await pushPage<void>(context, OtpScreen.signIn(challenge: challenge));
    } catch (e) {
      if (!mounted) return;
      setState(() => _error = errorText(context, e));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  void _fillDemo(String username) {
    _user.text = username;
    _pass.text = DemoRepository.demoPassword;
    setState(() => _error = null);
  }

  void _showHelp() {
    final s = S.of(context);
    showModalBottomSheet<void>(
      context: context,
      builder: (ctx) => SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(IdaSpace.s6, 0, IdaSpace.s6, IdaSpace.s6),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              IconBox(Icons.support_agent_rounded, size: IdaSizes.avatarMd),
              const SizedBox(height: IdaSpace.s4),
              Text(s.contactAdmin, style: ctx.text.titleMedium),
              const SizedBox(height: IdaSpace.s2),
              Text(s.contactAdminBody, style: ctx.text.bodyLarge!.copyWith(color: ctx.ida.textSecondary)),
              const SizedBox(height: IdaSpace.s5),
              PrimaryButton(label: s.close, onPressed: () => Navigator.pop(ctx)),
            ],
          ),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    final app = AppScope.of(context);
    final top = MediaQuery.paddingOf(context).top;

    return AnnotatedRegion<SystemUiOverlayStyle>(
      value: SystemUiOverlayStyle.light,
      child: Scaffold(
        backgroundColor: p.surface,
        body: IdaHeroSurface(
          child: CustomScrollView(
            slivers: [
              SliverToBoxAdapter(
                child: Padding(
                  padding: EdgeInsets.fromLTRB(IdaSpace.s6, top + IdaSpace.s3, IdaSpace.s4, IdaSpace.s8),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Row(children: [Spacer(), LanguageToggle(onBrand: true)]),
                      const SizedBox(height: IdaSpace.s8),
                      const IdaLogo.lockup(height: IdaSizes.logoLockup, reversed: true),
                      const SizedBox(height: IdaSpace.s4),
                      Text(
                        s.en
                            ? 'Doctor fee management for\nPhyathai–Paolo Hospital Group'
                            : 'ระบบบริหารค่าตอบแทนแพทย์\nเครือโรงพยาบาลพญาไท-เปาโล',
                        style: context.text.bodyLarge!.copyWith(color: IdaColors.textInverse, height: 1.5),
                      ),
                    ],
                  ),
                ),
              ),
              SliverToBoxAdapter(
                child: Container(
                  decoration: BoxDecoration(color: p.surface, borderRadius: IdaRadius.sheetR),
                  padding: const EdgeInsets.fromLTRB(IdaSpace.s6, IdaSpace.s8, IdaSpace.s6, IdaSpace.s6),
                  child: ContentWidth(
                    child: AutofillGroup(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          Semantics(header: true, child: Text(s.signIn, style: context.text.headlineSmall)),
                          const SizedBox(height: IdaSpace.s1),
                          Text(s.signInSubtitle, style: context.text.bodyMedium!.copyWith(color: p.textSecondary)),
                          const SizedBox(height: IdaSpace.s6),
                          IdaTextField(
                            fieldKey: const Key('username'),
                            label: s.username,
                            hint: s.usernameHint,
                            controller: _user,
                            prefixIcon: Icons.person_outline_rounded,
                            textInputAction: TextInputAction.next,
                            autofillHints: const [AutofillHints.username],
                            inputFormatters: [UpperCaseFormatter()],
                            onSubmitted: (_) => _passFocus.requestFocus(),
                          ),
                          const SizedBox(height: IdaSpace.s4),
                          IdaTextField(
                            fieldKey: const Key('password'),
                            label: s.password,
                            hint: s.passwordHint,
                            controller: _pass,
                            focusNode: _passFocus,
                            obscure: true,
                            prefixIcon: Icons.lock_outline_rounded,
                            textInputAction: TextInputAction.done,
                            autofillHints: const [AutofillHints.password],
                            onSubmitted: (_) => _submit(),
                          ),
                          const SizedBox(height: IdaSpace.s2),
                          Row(
                            children: [
                              Expanded(
                                child: InkWell(
                                  borderRadius: IdaRadius.mdR,
                                  onTap: () => setState(() => _remember = !_remember),
                                  child: ConstrainedBox(
                                    constraints: const BoxConstraints(minHeight: IdaSizes.minTapTarget),
                                    child: Row(
                                      children: [
                                        Checkbox(
                                          value: _remember,
                                          onChanged: (v) => setState(() => _remember = v ?? false),
                                        ),
                                        Flexible(child: Text(s.rememberUsername, style: context.text.bodyMedium)),
                                      ],
                                    ),
                                  ),
                                ),
                              ),
                              TextButton(
                                onPressed: () =>
                                    pushPage<void>(context, ForgotPasswordScreen(initialUsername: _user.text)),
                                child: Text(s.forgotPassword),
                              ),
                            ],
                          ),
                          if (_error != null) ...[const SizedBox(height: IdaSpace.s2), _ErrorBanner(_error!)],
                          const SizedBox(height: IdaSpace.s4),
                          PrimaryButton(
                            key: const Key('sign-in'),
                            label: s.signIn,
                            loading: _busy,
                            icon: Icons.login_rounded,
                            onPressed: _submit,
                          ),
                          if (app.isDemo) ...[const SizedBox(height: IdaSpace.s5), _DemoAccounts(onPick: _fillDemo)],
                          const SizedBox(height: IdaSpace.s8),
                          Wrap(
                            alignment: WrapAlignment.center,
                            crossAxisAlignment: WrapCrossAlignment.center,
                            children: [
                              Text(s.signInHelp, style: context.text.bodyMedium!.copyWith(color: p.textSecondary)),
                              TextButton(onPressed: _showHelp, child: Text(s.contactAdmin)),
                            ],
                          ),
                          Text(
                            '${s.version} 1.0.0${app.isDemo ? ' · ${s.demoMode}' : ''}',
                            textAlign: TextAlign.center,
                            style: context.text.bodySmall,
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
              ),


              SliverFillRemaining(hasScrollBody: false, child: ColoredBox(color: p.surface)),
            ],
          ),
        ),
      ),
    );
  }
}

class _ErrorBanner extends StatelessWidget {
  const _ErrorBanner(this.message);
  final String message;

  @override
  Widget build(BuildContext context) {
    final p = context.ida;
    return Semantics(
      liveRegion: true,
      child: Container(
        padding: const EdgeInsets.all(IdaSpace.s3),
        decoration: BoxDecoration(color: p.dangerSoft, borderRadius: IdaRadius.mdR),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(Icons.error_outline_rounded, color: p.dangerText, size: IdaSizes.iconMd),
            const SizedBox(width: IdaSpace.s2),
            Expanded(
              child: Text(message, style: context.text.bodyMedium!.copyWith(color: p.dangerText)),
            ),
          ],
        ),
      ),
    );
  }
}

class _DemoAccounts extends StatelessWidget {
  const _DemoAccounts({required this.onPick});
  final ValueChanged<String> onPick;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    Widget chip(String user, String role, IconData icon) => Expanded(
      child: Material(
        color: p.surface,
        shape: RoundedRectangleBorder(
          borderRadius: IdaRadius.lgR,
          side: BorderSide(color: p.border),
        ),
        child: InkWell(
          key: Key('demo-$user'),
          borderRadius: IdaRadius.lgR,
          onTap: () => onPick(user),
          child: Padding(
            padding: const EdgeInsets.symmetric(vertical: IdaSpace.s3, horizontal: IdaSpace.s2),
            child: Column(
              children: [
                Icon(icon, color: p.primaryText, size: IdaSizes.iconMd),
                const SizedBox(height: IdaSpace.s1),
                Text(user, style: idaNumeric(context.text.labelLarge!)),
                Text(role, textAlign: TextAlign.center, maxLines: 2, style: context.text.bodySmall),
              ],
            ),
          ),
        ),
      ),
    );
    return Container(
      padding: const EdgeInsets.all(IdaSpace.s3),
      decoration: BoxDecoration(color: p.accentSoft, borderRadius: IdaRadius.lgR),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(Icons.science_outlined, size: IdaSizes.iconSm, color: p.accentStrong),
              const SizedBox(width: IdaSpace.s1),
              Flexible(
                child: Text(
                  '${s.demoMode} · ${s.demoAccounts}',
                  style: context.text.labelMedium!.copyWith(color: p.accentStrong),
                ),
              ),
            ],
          ),
          const SizedBox(height: IdaSpace.s1),
          Text(
            s.demoCredentials(DemoRepository.demoPassword, DemoRepository.demoOtp),
            style: idaNumeric(context.text.bodySmall!).copyWith(color: p.accentStrong),
          ),
          const SizedBox(height: IdaSpace.s2),
          Row(
            children: [
              chip('D10001', s.demoDoctor, Icons.medical_services_outlined),
              const SizedBox(width: IdaSpace.s2),
              chip('A20001', s.demoAccounting, Icons.calculate_outlined),
              const SizedBox(width: IdaSpace.s2),
              chip('M30001', s.demoMdOffice, Icons.apartment_outlined),
            ],
          ),
        ],
      ),
    );
  }
}


class UpperCaseFormatter extends TextInputFormatter {
  @override
  TextEditingValue formatEditUpdate(TextEditingValue oldValue, TextEditingValue newValue) =>
      newValue.copyWith(text: newValue.text.toUpperCase());
}
