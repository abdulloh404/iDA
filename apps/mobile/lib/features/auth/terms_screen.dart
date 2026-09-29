import 'package:flutter/material.dart';

import '../../app/app_controller.dart';
import '../../core/format.dart';
import '../../data/models.dart';
import '../../l10n/strings.dart';
import '../../ui/components.dart';
import 'auth_common.dart';


class TermsScreen extends StatefulWidget {
  const TermsScreen({super.key, this.flow});
  final OnboardingFlow? flow;

  @override
  State<TermsScreen> createState() => _TermsScreenState();
}

class _TermsScreenState extends State<TermsScreen> {
  final _scroll = ScrollController();
  late final Future<TermsDocument> _doc = AppScope.read(context).repo.terms();
  double _progress = 0;
  bool _measured = false;
  bool _busy = false;

  bool get _atEnd => _progress >= 0.98;

  @override
  void initState() {
    super.initState();
    _scroll.addListener(_onScroll);
  }

  void _onScroll() {
    final max = _scroll.position.maxScrollExtent;
    final v = max <= 0 ? 1.0 : (_scroll.offset / max).clamp(0.0, 1.0);

    final crossed = (v >= 0.98) != _atEnd;
    if ((v - _progress).abs() > 0.01 || crossed) setState(() => _progress = v);
  }

  @override
  void dispose() {
    _scroll.dispose();
    super.dispose();
  }

  Future<void> _accept() async {
    setState(() => _busy = true);
    try {
      await AppScope.read(context).repo.acceptTerms();
      if (!mounted) return;
      widget.flow!.advance(context);
    } catch (e) {
      if (mounted) showErrorToast(context, e);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _decline() async {
    final s = S.of(context);
    final ok = await confirmDialog(
      context,
      title: s.termsDeclineTitle,
      body: s.termsDeclineBody,
      confirmLabel: s.termsDecline,
      destructive: true,
      icon: Icons.logout_rounded,
    );
    if (ok && mounted) await widget.flow!.abort(context);
  }

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    final onboarding = widget.flow != null;
    return PopScope(
      canPop: !onboarding,
      onPopInvokedWithResult: (didPop, _) {
        if (!didPop) _decline();
      },
      child: Scaffold(
        appBar: AppBar(
          title: Text(s.termsTitle),
          automaticallyImplyLeading: !onboarding,
          bottom: PreferredSize(
            preferredSize: const Size.fromHeight(IdaSizes.brandBar),
            child: LinearProgressIndicator(
              value: _progress,
              minHeight: IdaSizes.brandBar,
              backgroundColor: p.primaryActive,
              color: p.accent,
              semanticsLabel: s.termsReadHint,
            ),
          ),
        ),
        body: FutureBuilder<TermsDocument>(
          future: _doc,
          builder: (context, snap) {
            if (snap.hasError) {
              return ErrorState(error: snap.error!, onRetry: () => setState(() {}));
            }
            if (!snap.hasData) return const SkeletonList(count: 6);
            final doc = snap.data!;

            if (!_measured) {
              _measured = true;
              WidgetsBinding.instance.addPostFrameCallback((_) {
                if (_scroll.hasClients) _onScroll();
              });
            }


            return SingleChildScrollView(
              controller: _scroll,
              padding: const EdgeInsets.fromLTRB(IdaSpace.s5, IdaSpace.s6, IdaSpace.s5, IdaSpace.s8),
              child: ContentWidth(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    if (onboarding && widget.flow!.stepLabel(s) != null)
                      Text(
                        widget.flow!.stepLabel(s)!,
                        style: context.text.labelMedium!.copyWith(color: p.accentStrong),
                      ),
                    Text(s.termsTitle, style: context.text.headlineSmall),
                    const SizedBox(height: IdaSpace.s2),
                    const IdaBrandTick(),
                    const SizedBox(height: IdaSpace.s2),
                    Text(s.termsVersion(doc.version, Fmt.date(doc.updatedAt, s.en)), style: context.text.bodySmall),
                    const SizedBox(height: IdaSpace.s5),
                    for (final sec in doc.sections) ...[
                      Text(sec.title, style: context.text.titleSmall),
                      const SizedBox(height: IdaSpace.s2),
                      Text(sec.body, style: context.text.bodyLarge!.copyWith(color: p.textSecondary, height: 1.7)),
                      const SizedBox(height: IdaSpace.s5),
                    ],
                  ],
                ),
              ),
            );
          },
        ),
        bottomNavigationBar: !onboarding
            ? null
            : BottomActionBar(
                child: Row(
                  children: [
                    Expanded(
                      child: TextButton(onPressed: _busy ? null : _decline, child: Text(s.termsDecline)),
                    ),
                    const SizedBox(width: IdaSpace.s3),
                    Expanded(
                      flex: 2,
                      child: _atEnd
                          ? PrimaryButton(
                              key: const Key('terms-accept'),
                              label: s.termsAccept,
                              icon: Icons.check_rounded,
                              loading: _busy,
                              onPressed: _accept,
                            )
                          : OutlinedButton.icon(
                              key: const Key('terms-scroll'),
                              onPressed: () {
                                final d = IdaMotion.of(context, IdaMotion.slow);
                                final end = _scroll.position.maxScrollExtent;

                                d == Duration.zero
                                    ? _scroll.jumpTo(end)
                                    : _scroll.animateTo(end, duration: d, curve: IdaMotion.ease);
                              },
                              icon: const Icon(Icons.keyboard_double_arrow_down_rounded, size: IdaSizes.iconMd),
                              label: Text(s.termsScrollDown),
                            ),
                    ),
                  ],
                ),
              ),
      ),
    );
  }
}
