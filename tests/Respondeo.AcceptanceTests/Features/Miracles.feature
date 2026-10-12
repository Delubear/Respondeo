Feature: Miracles
  As a visitor
  I want to browse documented miracles
  So that I can read an account and its key facts

  Scenario: The miracles browser lists miracle cards
	Given I open the miracles browser
	Then I should see at least one miracle card

  Scenario: Opening a miracle from the miracles browser
	Given I open the miracles browser
	When I choose the first miracle
	Then the miracle account should be visible
	And the miracle facts should be visible
