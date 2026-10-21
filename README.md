# Scenario Constructor

A Unity tool for building training scenarios from reusable steps. Scenarios are configured in the Inspector and support different execution modes, event sources, loggers, and scoring algorithms without duplicating step assets.

Built with **Unity 6000.4.4f1**, URP, and standard Unity packages. No third-party runtime frameworks are used.

## Getting started
1. Select **Scenario Constructor > Open Demo Scene** and enter Play Mode.
2. Choose **Training** or **Exam**. Use **Restart** to reset the current run.
3. Complete actions in the center panel. Tasks appear on the left; results and the event journal appear on the right.
4. After finishing, choose **Switch analyzer**, then **Re-evaluate** to analyze the same journal with another scoring algorithm.

## Execution rules

**Training:** all steps belong to one group. Failed attempts can be retried without resetting other active steps. Successful steps earn 10 points on attempt 1, 5 on attempt 2, and 0 on later attempts.

**Exam:** the same twelve steps are divided into three sequential groups: S01–S04, S05–S08, and S09–S12. A violation makes its step Failed. At the group's error limit, remaining unfinished steps become Skipped. The default limit is 2. The score starts at 100, subtracts 10 per Failed step and 5 per Skipped step, and cannot fall below zero.

Activation is configured separately from grouping. The supplied sequence is S01–S04 together, S05 through S08 individually, S09–S10 together, then S11–S12 together. Actions on pending steps count as violations by default; profiles can instead ignore them.

A premature action targeting a future exam group is charged to that group, whose threshold is checked when it becomes current. The result panel retains the last completed score during a new run; its run ID identifies which journal was evaluated.

## Editing scenarios

Configuration assets are in `Assets/ScenarioConstructor/Examples/Inspection`.

- Open **Training** or **Exam** to edit the shared step roster, groups, activation batches, pending-action behavior, execution, logger, and analyzer.
- Drag **Activation Batches** to change execution order. Members of one batch activate together. Every step must appear once in the groups and once in the batches; batch order must follow group order.
- Edit a step's **Rule** to configure ordered action IDs or a required boolean state. Edit **Exam Execution** to change the error limit.
- Connect scene events through **ScenarioActionRelay**: assign the session, step, action ID, and optional state flags. Use ButtonActionSource, TriggerActionSource, CollisionActionSource, or connect a UnityEvent to `Raise()`.
- Action IDs must exactly match the selected rule. Update scene bindings when replacing demo step assets. Start the mode again to apply edited configuration.

Create additional assets through **Assets > Create > Scenario Constructor**. The profile Inspector reports configuration errors.

## Validation and limitations

Open **Scenario Constructor > Test Runner** to run **67 EditMode** and **13 PlayMode** tests. They cover the 15-point training and 70-point exam examples, activation, journal compatibility, event sources, and restart. PlayMode tests require graphics. A Windows x64 Development/Mono build was also validated.

