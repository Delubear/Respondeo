Feature: Pinned help orbs in the quick menu
  As a returning visitor
  I want to pin a help guide to the quick menu
  So that I can open it from any page

  Scenario: A pinned orb opens its guide dialog
	Given I open the start page
	And the "prayers" guide orb is pinned in the quick menu
	When I open the quick menu
	And I choose the "Guide to Prayers" pin
	Then the prayer guide dialog should be visible
	And the prayer guide dialog should explain the versicle and response
